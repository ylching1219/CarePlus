namespace CarePlus.API.DTO.CartDTO
{
    public class CartQtyRequest
    {
        public string MedicineId { get; set; } = "";
        public int Qty { get; set; }
    }

    public class CartRemoveRequest
    {
        public List<string> MedicineIds { get; set; } = [];
    }

    public class CartLineResponse
    {
        public string MedicineId { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
    }

    public class MonthlyBoughtResponse
    {
        public string MedicineId { get; set; } = "";
        public int Qty { get; set; }
    }

    public class CartStateResponse
    {
        public List<CartLineResponse> Lines { get; set; } = [];
        public List<MonthlyBoughtResponse> Bought { get; set; } = [];
    }
}
