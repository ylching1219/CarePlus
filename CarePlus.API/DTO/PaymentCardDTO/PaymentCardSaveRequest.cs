namespace CarePlus.API.DTO.PaymentCardDTO
{
    public class PaymentCardSaveRequest
    {
        public string? Number { get; set; }
        public string? Expiry { get; set; }
        public string? HolderName { get; set; }
        public string? Cvv { get; set; }
    }
}
