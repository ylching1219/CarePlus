namespace CarePlus.API.Services.JWTServices.Interface
{
    public interface IJWTServices
    {
        string GenerateToken(
            long userId, 
            string username, 
            string accountType,
            string role, 
            string? patientId = null, 
            string? staffId = null
        );
    }
}
