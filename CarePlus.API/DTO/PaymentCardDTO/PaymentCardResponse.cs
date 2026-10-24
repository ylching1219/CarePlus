namespace CarePlus.API.DTO.PaymentCardDTO
{
    public class PaymentCardResponse
    {
        public long Id { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Last4 { get; set; } = string.Empty;
        public string Expiry { get; set; } = string.Empty;
        public string HolderName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
