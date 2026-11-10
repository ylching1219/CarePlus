namespace CarePlus.API.DTO.QueueDTO
{
    public class QueueOverviewResponse
    {
        public QueueStatusResponse? ActiveQueue { get; set; }
        public QueueCheckInResponse? CheckIn { get; set; }
    }

    public class QueueStatusResponse
    {
        public string QueueId { get; set; } = "";
        public string QueueNumber { get; set; } = "";
        public string AppointmentId { get; set; } = "";
        public string Status { get; set; } = "";
        public string CheckedInAt { get; set; } = "";
        public string AppointmentAt { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string VisitType { get; set; } = "";
        public string? RoomId { get; set; }
        public string? RoomName { get; set; }
        public int PeopleAhead { get; set; }
        public int EstimatedWaitMinutes { get; set; }
    }

    public class QueueCheckInResponse
    {
        public string AppointmentId { get; set; } = "";
        public string AppointmentAt { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string VisitType { get; set; } = "";
        public string CheckInOpensAt { get; set; } = "";
        public string CheckInClosesAt { get; set; } = "";
        public bool CanCheckIn { get; set; }
        public string Message { get; set; } = "";
    }

    public class QueueStatusUpdateRequest
    {
        public string Status { get; set; } = "";
    }
}
