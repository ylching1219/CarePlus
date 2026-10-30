using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using CarePlus.API.Services.EmailServices.Interface;

namespace CarePlus.API.Services.EmailServices;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string recipient, string subject, string body, bool isBodyHtml = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException("Email delivery is not configured. Set Email__Host and Email__FromAddress in the API environment.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.DisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isBodyHtml,
        };
        message.To.Add(new MailAddress(recipient));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
