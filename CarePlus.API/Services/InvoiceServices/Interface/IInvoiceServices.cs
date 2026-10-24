using CarePlus.API.DTO.InvoiceDTO;

namespace CarePlus.API.Services.InvoiceServices.Interface
{
    public interface IInvoiceServices
    {
        Task<ApiResponse<List<OutstandingInvoiceResponse>>> ListOutstandingAsync(long userId);
        Task<ApiResponse<OutstandingInvoiceResponse>> PayAsync(long userId, string invoiceId, InvoicePayRequest request);
    }
}
