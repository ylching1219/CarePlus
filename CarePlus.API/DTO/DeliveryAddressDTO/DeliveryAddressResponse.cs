namespace CarePlus.API.DTO.DeliveryAddressDTO
{
    public class DeliveryAddressResponse
    {
        public long Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
