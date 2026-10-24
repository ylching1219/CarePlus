namespace CarePlus.API.DTO.MedicineDTO
{
    public class MedicineRetrieveResponse
    {
        public string MedicineId { get; set; } = string.Empty;
        public string MedicineName { get; set; } = string.Empty;
        public string? NameZh { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? DescriptionZh { get; set; }
        public string Pack { get; set; } = string.Empty;
        public string? PackZh { get; set; }
        public string Form { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string? MethodZh { get; set; }
        public string ImageKey { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
