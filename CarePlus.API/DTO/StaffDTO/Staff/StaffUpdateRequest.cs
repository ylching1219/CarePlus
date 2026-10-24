namespace CarePlus.API.DTO.StaffDTO
{
    public record StaffUpdateRequest : StaffUserUpdateRequest
    {
        public string? Role { get; init; }
    }
}
