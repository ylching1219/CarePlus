namespace CarePlus.API.Services.EmailServices;

public class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "CarePlus Clinic";
    public bool UseSsl { get; set; } = true;
}
