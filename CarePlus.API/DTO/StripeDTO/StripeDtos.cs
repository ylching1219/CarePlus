namespace CarePlus.API.DTO.StripeDTO
{
    public class CreateInvoiceCheckoutRequest
    {
        public string InvoiceId { get; set; } = "";
        public string Method { get; set; } = "card";
        public string MethodLabel { get; set; } = "";
        public string SuccessPath { get; set; } = "/pay/stripe/return";
        public string CancelPath { get; set; } = "/invoices";
        public string? ReturnOrigin { get; set; }
    }

    public class CreateMedicineCheckoutRequest
    {
        public decimal Amount { get; set; }
        /// <summary>When set, charge this saved order instead of the cart.</summary>
        public string? OrderId { get; set; }
        public string Method { get; set; } = "card";
        public string MethodLabel { get; set; } = "";
        public string SuccessPath { get; set; } = "/pay/stripe/return";
        public string CancelPath { get; set; } = "/checkout";
        public string? ReturnOrigin { get; set; }
    }

    public class CompleteCheckoutRequest
    {
        public string SessionId { get; set; } = "";
    }

    public class CheckoutSessionResponse
    {
        public string SessionId { get; set; } = "";
        public string Url { get; set; } = "";
        public string PublishableKey { get; set; } = "";
    }

    public class CompletedCheckoutResponse
    {
        public string Kind { get; set; } = "";
        public string Method { get; set; } = "";
        public string MethodLabel { get; set; } = "";
        public string? InvoiceId { get; set; }
        public string? OrderId { get; set; }
        public decimal Amount { get; set; }
        public bool Paid { get; set; }
    }
}
