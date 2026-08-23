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

        var isCritical = notification.AlertType.Contains("OutOfRange", StringComparison.OrdinalIgnoreCase);
        var isReminder = notification.Message.Contains("still outside", StringComparison.OrdinalIgnoreCase);
        var subject = isCritical
            ? isReminder
                ? $"[ALERT] {notification.DeviceName} temperature still out of range"
                : $"[ALERT] {notification.DeviceName} temperature out of range"
            : $"[OK] {notification.DeviceName} temperature normalized";

        var htmlBody = $"""
            <h2>{subject}</h2>
            <p><strong>Device:</strong> {notification.DeviceName}</p>
            <p><strong>Zone:</strong> {notification.ZoneName}</p>
            <p><strong>Message:</strong> {notification.Message}</p>
            <p><strong>Temperature:</strong> {notification.TemperatureC}°C</p>
            <p><strong>Time (UTC):</strong> {notification.TriggeredAtUtc:yyyy-MM-dd HH:mm:ss}</p>
            <hr />
            <p style="color:#666;font-size:12px;">SmartMonitoring alert notification</p>
            """;

        var sent = 0;
        foreach (var recipient in recipients)
        {
            await emailSender.SendEmailAsync(recipient, subject, htmlBody, cancellationToken);
            sent++;
        }

        return ResponseDto<int>.SuccessResponse(sent, $"Sent {sent} notification email(s).");
    }
}
