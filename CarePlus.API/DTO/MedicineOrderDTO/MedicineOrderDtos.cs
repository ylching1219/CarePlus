namespace CarePlus.API.DTO.MedicineOrderDTO
{
    public class MedicineOrderPlaceRequest
    {
        public string Fulfilment { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public long? DeliveryAddressId { get; set; }
        public decimal DeliveryFee { get; set; }
        public List<MedicineOrderLineRequest> Lines { get; set; } = [];
    }

    public class MedicineOrderLineRequest
    {
        public string? MedicineId { get; set; }
        public string Name { get; set; } = "";
        public string? Detail { get; set; }
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
    }

    public class MedicineOrderResponse
    {
        public string Id { get; set; } = "";
        public string PlacedAt { get; set; } = "";
        public string Status { get; set; } = "";
        public string Fulfilment { get; set; } = "";
        public string Payment { get; set; } = "";
        public string? Address { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<MedicineOrderLineResponse> Lines { get; set; } = [];
    }

    public class MedicineOrderLineResponse
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Detail { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
    }

    public class MapPointResponse
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
