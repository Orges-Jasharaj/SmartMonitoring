using MediatR;
using NotificationService.Services.Email;
using SmartMonitoring.Shared.Dtos.Responses;
using SmartMonitoring.Shared.Notifications;

namespace NotificationService.Features.SendAlert;

public class SendAlertNotificationCommand : IRequest<ResponseDto<int>>
{
    public AlertNotificationRequest Notification { get; set; } = null!;
}

public class SendAlertNotificationHandler(IEmailSender emailSender) : IRequestHandler<SendAlertNotificationCommand, ResponseDto<int>>
{
    public async Task<ResponseDto<int>> Handle(SendAlertNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = request.Notification;
        var recipients = notification.RecipientEmails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            return ResponseDto<int>.Failure("No recipient emails were provided.");
        }

        var subject = AlertEmailFormatter.BuildSubject(notification);
        var htmlBody = AlertEmailFormatter.BuildHtmlBody(notification, subject);

        var sent = 0;
        foreach (var recipient in recipients)
        {
            await emailSender.SendEmailAsync(recipient, subject, htmlBody, cancellationToken);
            sent++;
        }

        return ResponseDto<int>.SuccessResponse(sent, $"Sent {sent} notification email(s).");
    }
}
