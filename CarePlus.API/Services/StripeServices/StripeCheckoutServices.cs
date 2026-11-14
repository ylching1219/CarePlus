using CarePlus.API.Data;
using CarePlus.API.DTO.InvoiceDTO;
using CarePlus.API.DTO.StripeDTO;
using CarePlus.API.Options;
using CarePlus.API.Services.CartServices.Interface;
using CarePlus.API.Services.InvoiceServices.Interface;
using CarePlus.API.Services.StripeServices.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CarePlus.API.Services.StripeServices
{
    public class StripeCheckoutServices : IStripeCheckoutServices
    {
        private static readonly HashSet<string> AllowedOrigins = new(StringComparer.OrdinalIgnoreCase)
        {
            "http://localhost:5173",
            "http://localhost:5174",
            "http://127.0.0.1:5173",
            "http://127.0.0.1:5174",
        };

        private readonly IInvoiceServices _invoices;
        private readonly ICartServices _cart;
        private readonly AppDbContext _db;
        private readonly StripeOptions _stripe;
        private readonly IConfiguration _config;

        public StripeCheckoutServices(
            IInvoiceServices invoices,
            ICartServices cart,
            AppDbContext db,
            IOptions<StripeOptions> stripe,
            IConfiguration config)
        {
            _invoices = invoices;
            _cart = cart;
            _db = db;
            _stripe = stripe.Value;
            _config = config;
        }

        public async Task<ApiResponse<CheckoutSessionResponse>> CreateInvoiceCheckoutAsync(long userId, CreateInvoiceCheckoutRequest request)
        {
            var setup = EnsureConfigured();
            if (setup != null) return setup;

            var invoiceId = request.InvoiceId?.Trim() ?? "";
            if (invoiceId.Length == 0)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("Choose an invoice.", 400);
            }

            var method = NormalizeMethod(request.Method);
            if (method == null)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("Choose card or FPX.", 400);
            }

            var outstanding = await _invoices.ListOutstandingAsync(userId);
            if (!outstanding.IsSuccess || outstanding.Data == null)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail(
                    string.IsNullOrWhiteSpace(outstanding.RespMessage) ? "Invoice list failed." : outstanding.RespMessage,
                    outstanding.RespCode);
            }

            var invoice = outstanding.Data.FirstOrDefault(row => row.Id == invoiceId);
            if (invoice == null)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("That invoice is not open.", 400);
            }

            var amountCents = ToCents(invoice.AmountDue);
            if (amountCents < 200)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("Amount is too small for Stripe Checkout.", 400);
            }

            var methodLabel = string.IsNullOrWhiteSpace(request.MethodLabel)
                ? (method == "fpx" ? "FPX · Stripe" : "Card · Stripe")
                : request.MethodLabel.Trim();

            var origin = ResolveOrigin(request.ReturnOrigin);
            if (origin == null)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("Return origin is not allowed.", 400);
            }

            var successPath = NormalizePath(request.SuccessPath, "/pay/stripe/return");
            var cancelPath = NormalizePath(request.CancelPath, $"/invoices/{invoiceId}/pay");

            try
            {
                StripeConfiguration.ApiKey = _stripe.SecretKey;
                var session = await new SessionService().CreateAsync(new SessionCreateOptions
                {
                    Mode = "payment",
                    SuccessUrl = $"{origin}{successPath}?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{origin}{cancelPath}",
                    ClientReferenceId = userId.ToString(),
                    PaymentMethodTypes = new List<string> { method },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new()
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = Currency(),
                                UnitAmount = amountCents,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"CarePlus invoice {invoiceId}",
                                    Description = $"{invoice.DoctorName} · {invoice.VisitType}".Trim(' ', '·'),
                                },
                            },
                        },
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["kind"] = "invoice",
                        ["userId"] = userId.ToString(),
                        ["invoiceId"] = invoiceId,
                        ["method"] = method,
                        ["methodLabel"] = Truncate(methodLabel, 80),
                        ["amountCents"] = amountCents.ToString(),
                    },
                });

                if (string.IsNullOrWhiteSpace(session.Url))
                {
                    return ApiResponse<CheckoutSessionResponse>.Fail("Stripe did not return a checkout URL.", 502);
                }

                return ApiResponse<CheckoutSessionResponse>.Ok(new CheckoutSessionResponse
                {
                    SessionId = session.Id,
                    Url = session.Url,
                    PublishableKey = _stripe.PublishableKey,
                });
            }
            catch (StripeException)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("Payment provider error.", 502);
            }
        }

        public async Task<ApiResponse<CheckoutSessionResponse>> CreateMedicineCheckoutAsync(long userId, CreateMedicineCheckoutRequest request)
        {
            var setup = EnsureConfigured();
            if (setup != null) return setup;

            var method = NormalizeMethod(request.Method);
            if (method == null)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("Choose card or FPX.", 400);
            }

            // ponytail: ignore client Amount. A saved order is charged its own total; otherwise the cart.
            var orderId = request.OrderId?.Trim() ?? "";
            decimal amount;
            if (orderId.Length > 0)
            {
                var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(row => row.UserId == userId);
                if (patient == null)
                {
                    return ApiResponse<CheckoutSessionResponse>.Fail("This account is not a patient account.", 400);
                }

                var order = await _db.MedicineOrders.AsNoTracking()
                    .FirstOrDefaultAsync(row => row.OrderId == orderId && row.PatientId == patient.PatientId);
                if (order == null)
                {
                    return ApiResponse<CheckoutSessionResponse>.Fail("Order not found.", 404);
                }

                if (!string.Equals(order.Status, "awaiting_payment", StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<CheckoutSessionResponse>.Fail("This order is not waiting for payment.", 400);
                }

                amount = order.TotalAmount;
            }
            else
            {
                var cart = await _cart.ListAsync(userId);
                if (!cart.IsSuccess || cart.Data == null || cart.Data.Lines.Count == 0)
                {
                    return ApiResponse<CheckoutSessionResponse>.Fail("Your cart is empty.", 400);
                }

                amount = cart.Data.Lines.Sum(line => line.UnitPrice * line.Qty);
            }
            var amountCents = ToCents(amount);
            if (amountCents < 200)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("Amount is too small for Stripe Checkout.", 400);
            }

            var methodLabel = string.IsNullOrWhiteSpace(request.MethodLabel)
                ? (method == "fpx" ? "FPX · Stripe" : "Card · Stripe")
                : request.MethodLabel.Trim();

            var origin = ResolveOrigin(request.ReturnOrigin);
            if (origin == null)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("Return origin is not allowed.", 400);
            }

            var successPath = NormalizePath(request.SuccessPath, "/pay/stripe/return");
            var cancelPath = NormalizePath(request.CancelPath, "/checkout");
            var metadata = new Dictionary<string, string>
            {
                ["kind"] = "medicine",
                ["userId"] = userId.ToString(),
                ["method"] = method,
                ["methodLabel"] = Truncate(methodLabel, 80),
                ["amountCents"] = amountCents.ToString(),
            };
            if (orderId.Length > 0)
            {
                metadata["orderId"] = orderId;
            }

            try
            {
                StripeConfiguration.ApiKey = _stripe.SecretKey;
                var session = await new SessionService().CreateAsync(new SessionCreateOptions
                {
                    Mode = "payment",
                    SuccessUrl = $"{origin}{successPath}?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{origin}{cancelPath}",
                    ClientReferenceId = userId.ToString(),
                    PaymentMethodTypes = new List<string> { method },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new()
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = Currency(),
                                UnitAmount = amountCents,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "CarePlus medicine order",
                                },
                            },
                        },
                    },
                    Metadata = metadata,
                });

                if (string.IsNullOrWhiteSpace(session.Url))
                {
                    return ApiResponse<CheckoutSessionResponse>.Fail("Stripe did not return a checkout URL.", 502);
                }

                return ApiResponse<CheckoutSessionResponse>.Ok(new CheckoutSessionResponse
                {
                    SessionId = session.Id,
                    Url = session.Url,
                    PublishableKey = _stripe.PublishableKey,
                });
            }
            catch (StripeException)
            {
                return ApiResponse<CheckoutSessionResponse>.Fail("Payment provider error.", 502);
            }
        }

        public async Task<ApiResponse<CompletedCheckoutResponse>> CompleteCheckoutAsync(long userId, CompleteCheckoutRequest request)
        {
            var setupComplete = EnsureConfiguredComplete();
            if (setupComplete != null) return setupComplete;

            var sessionId = request.SessionId?.Trim() ?? "";
            if (sessionId.Length == 0)
            {
                return ApiResponse<CompletedCheckoutResponse>.Fail("Missing Stripe session.", 400);
            }

            Session session;
            try
            {
                StripeConfiguration.ApiKey = _stripe.SecretKey;
                session = await new SessionService().GetAsync(sessionId);
            }
            catch (StripeException)
            {
                return ApiResponse<CompletedCheckoutResponse>.Fail("Payment provider error.", 502);
            }

            if (!string.Equals(session.ClientReferenceId, userId.ToString(), StringComparison.Ordinal)
                && (!session.Metadata.TryGetValue("userId", out var metaUser) || metaUser != userId.ToString()))
            {
                return ApiResponse<CompletedCheckoutResponse>.Fail("This checkout belongs to another account.", 403);
            }

            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(session.Status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<CompletedCheckoutResponse>.Fail("Payment is not complete yet.", 400);
            }

            session.Metadata.TryGetValue("kind", out var kind);
            session.Metadata.TryGetValue("method", out var method);
            session.Metadata.TryGetValue("methodLabel", out var methodLabel);
            session.Metadata.TryGetValue("invoiceId", out var invoiceId);
            session.Metadata.TryGetValue("orderId", out var orderId);
            session.Metadata.TryGetValue("amountCents", out var amountCentsRaw);
            _ = long.TryParse(amountCentsRaw, out var amountCents);
            var amount = amountCents / 100m;
            kind ??= "";
            method ??= "card";
            methodLabel = string.IsNullOrWhiteSpace(methodLabel)
                ? (method == "fpx" ? "FPX · Stripe" : "Card · Stripe")
                : methodLabel;
            var shortId = session.Id.Length > 8 ? session.Id[^8..] : session.Id;
            var paidLabel = Truncate($"{methodLabel} · Stripe {shortId}", 80);

            if (string.Equals(kind, "invoice", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(invoiceId))
                {
                    return ApiResponse<CompletedCheckoutResponse>.Fail("Invoice checkout is missing an invoice id.", 400);
                }

                var pay = await _invoices.PayAsync(userId, invoiceId, new InvoicePayRequest { PaymentMethod = paidLabel });
                if (!pay.IsSuccess)
                {
                    // Idempotent: already paid after a retry is fine.
                    if (pay.RespMessage?.Contains("already paid", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return ApiResponse<CompletedCheckoutResponse>.Ok(new CompletedCheckoutResponse
                        {
                            Kind = "invoice",
                            Method = method,
                            MethodLabel = paidLabel,
                            InvoiceId = invoiceId,
                            Amount = amount,
                            Paid = true,
                        });
                    }

                    return ApiResponse<CompletedCheckoutResponse>.Fail(
                        string.IsNullOrWhiteSpace(pay.RespMessage) ? "Invoice was not marked paid." : pay.RespMessage,
                        pay.RespCode);
                }

                return ApiResponse<CompletedCheckoutResponse>.Ok(new CompletedCheckoutResponse
                {
                    Kind = "invoice",
                    Method = method,
                    MethodLabel = paidLabel,
                    InvoiceId = invoiceId,
                    Amount = pay.Data?.AmountDue ?? amount,
                    Paid = true,
                });
            }

            if (string.Equals(kind, "medicine", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<CompletedCheckoutResponse>.Ok(new CompletedCheckoutResponse
                {
                    Kind = "medicine",
                    Method = method,
                    MethodLabel = paidLabel,
                    OrderId = string.IsNullOrWhiteSpace(orderId) ? null : orderId,
                    Amount = amount,
                    Paid = true,
                });
            }

            return ApiResponse<CompletedCheckoutResponse>.Fail("Unknown Stripe checkout kind.", 400);
        }

        private ApiResponse<CheckoutSessionResponse>? EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            {
                return ApiResponse<CheckoutSessionResponse>.Fail(
                    "Stripe is not configured. Add Stripe:SecretKey (sk_test_...) in appsettings.",
                    503);
            }

            return null;
        }

        private ApiResponse<CompletedCheckoutResponse>? EnsureConfiguredComplete()
        {
            if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            {
                return ApiResponse<CompletedCheckoutResponse>.Fail(
                    "Stripe is not configured. Add Stripe:SecretKey (sk_test_...) in appsettings.",
                    503);
            }

            return null;
        }

        private string Currency() =>
            string.IsNullOrWhiteSpace(_stripe.Currency) ? "myr" : _stripe.Currency.Trim().ToLowerInvariant();

        private string? ResolveOrigin(string? returnOrigin)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(returnOrigin))
            {
                candidates.Add(returnOrigin.Trim().TrimEnd('/'));
            }

            var configured = _config["Client:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                candidates.Add(configured.Trim().TrimEnd('/'));
            }

            foreach (var candidate in candidates)
            {
                if (AllowedOrigins.Contains(candidate))
                {
                    return candidate;
                }

                // Allow configured production origin when it is set explicitly.
                if (!string.IsNullOrWhiteSpace(configured)
                    && string.Equals(candidate, configured.Trim().TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
                    && Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.Host is "localhost" or "127.0.0.1"))
                {
                    return candidate;
                }
            }

            return AllowedOrigins.FirstOrDefault();
        }

        private static string? NormalizeMethod(string? method)
        {
            var value = method?.Trim().ToLowerInvariant() ?? "";
            return value is "card" or "fpx" ? value : null;
        }

        private static string NormalizePath(string? path, string fallback)
        {
            var value = string.IsNullOrWhiteSpace(path) ? fallback : path.Trim();
            if (!value.StartsWith('/'))
            {
                value = "/" + value;
            }

            if (value.Contains("://", StringComparison.Ordinal) || value.Contains('\\', StringComparison.Ordinal))
            {
                return fallback;
            }

            return value;
        }

        private static long ToCents(decimal amount) =>
            (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max];
    }
}
