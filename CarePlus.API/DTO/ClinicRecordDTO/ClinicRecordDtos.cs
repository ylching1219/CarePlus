namespace CarePlus.API.DTO.ClinicRecordDTO
{
    public class ClinicRecordResponse
    {
        public string Id { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string PatientMeta { get; set; } = "";
        public string DoctorId { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string DoctorPhoto { get; set; } = "";
        public string Specialty { get; set; } = "";
        public string VisitTypeId { get; set; } = "general";
        public string VisitType { get; set; } = "";
        public string VisitedAt { get; set; } = "";
        public string Status { get; set; } = "completed";
        public string Diagnosis { get; set; } = "";
        public string DoctorComment { get; set; } = "";
        public List<string> Medications { get; set; } = [];
        public string Ward { get; set; } = "";
        public List<ClinicChargeLineResponse> Charges { get; set; } = [];
        public decimal SstRate { get; set; }
    }

    public class ClinicChargeLineResponse
    {
        public string Id { get; set; } = "";
        public string Category { get; set; } = "other";
        public string Item { get; set; } = "";
        public string Detail { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
        public List<ClinicLabMarkerResponse>? Markers { get; set; }
    }

    public class ClinicLabMarkerResponse
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Range { get; set; } = "";
        public string Flag { get; set; } = "normal";
    }
}
