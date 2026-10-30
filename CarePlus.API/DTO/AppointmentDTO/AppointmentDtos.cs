namespace CarePlus.API.DTO.AppointmentDTO
{
    public class BookingDoctorResponse
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Specialty { get; set; } = "";
        public string Bio { get; set; } = "";
        public string Qualification { get; set; } = "";
        public int YearsOfExperience { get; set; }
    }

    public class BookingVisitTypeResponse
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class BookingCatalogResponse
    {
        public List<BookingDoctorResponse> Doctors { get; set; } = [];
        public List<BookingVisitTypeResponse> Types { get; set; } = [];
        public bool BookingClosed { get; set; }
        public int CancelsToday { get; set; }
        public int CancelLimit { get; set; }
        public int RescheduleLimit { get; set; }
    }

    public class BookingSlotResponse
    {
        public string StartAt { get; set; } = "";
        public string Label { get; set; } = "";
        public bool Booked { get; set; }
    }

    public class BookingSlotsResponse
    {
        public string Date { get; set; } = "";
        public string DoctorId { get; set; } = "";
        public List<BookingSlotResponse> Morning { get; set; } = [];
        public List<BookingSlotResponse> Night { get; set; } = [];
    }

    public class AppointmentResponse
    {
        public string Id { get; set; } = "";
        public string ForName { get; set; } = "";
        public string DoctorId { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string Type { get; set; } = "";
        public string? Notes { get; set; }
        public string StartAt { get; set; } = "";
        public string EndAt { get; set; } = "";
        public string Status { get; set; } = "";
        public int RescheduleCount { get; set; }
        public bool RescheduleBlocked { get; set; }
    }

    public class AppointmentBookRequest
    {
        public string DoctorId { get; set; } = "";
        public string StartAt { get; set; } = "";
        public string Type { get; set; } = "";
        public string? ForName { get; set; }
        public string? Notes { get; set; }
    }

    public class AppointmentRescheduleRequest
    {
        public string StartAt { get; set; } = "";
    }
}
