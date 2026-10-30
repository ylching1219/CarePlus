namespace CarePlus.API.Services.EmailServices.Interface;

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, bool isBodyHtml = false, CancellationToken cancellationToken = default);
}
