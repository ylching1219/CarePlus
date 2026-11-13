using CarePlus.API.DTO.StripeDTO;

namespace CarePlus.API.Services.StripeServices.Interface
{
    public interface IStripeCheckoutServices
    {
        Task<ApiResponse<CheckoutSessionResponse>> CreateInvoiceCheckoutAsync(long userId, CreateInvoiceCheckoutRequest request);
        Task<ApiResponse<CheckoutSessionResponse>> CreateMedicineCheckoutAsync(long userId, CreateMedicineCheckoutRequest request);
        Task<ApiResponse<CompletedCheckoutResponse>> CompleteCheckoutAsync(long userId, CompleteCheckoutRequest request);
    }
}
