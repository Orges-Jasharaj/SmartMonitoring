using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace IdentityService.Services.Email;

public class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var emailOptions = options.Value;
        if (!emailOptions.Enabled)
        {
            logger.LogInformation("Email sending is disabled. Skipped email to {Recipient} with subject {Subject}", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(emailOptions.FromName, emailOptions.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(emailOptions.Host, emailOptions.Port, emailOptions.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);

        if (!string.IsNullOrWhiteSpace(emailOptions.UserName))
        {
            await client.AuthenticateAsync(emailOptions.UserName, emailOptions.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Email sent to {Recipient} with subject {Subject}", to, subject);
    }
}
