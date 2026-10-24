namespace CarePlus.API.DTO.DeliveryAddressDTO
{
    public class DeliveryAddressSaveRequest
    {
        public string Label { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class DeliveryAddressDefaultRequest
    {
        public long? Id { get; set; }
    }
}
