using SmartMonitoring.Shared.Notifications;

namespace NotificationService.Features.SendAlert;

internal static class AlertEmailFormatter
{
    public static string BuildSubject(AlertNotificationRequest notification)
    {
        var label = GetMetricLabel(notification.AlertType);
        var isReminder = notification.Message.Contains("still", StringComparison.OrdinalIgnoreCase);

        if (notification.AlertType.Contains("Normalized", StringComparison.Ordinal))
        {
            return $"[OK] {notification.DeviceName} — {label} back to normal";
        }

        if (notification.AlertType.Equals("LowBattery", StringComparison.Ordinal))
        {
            return isReminder
                ? $"[ALERT] {notification.DeviceName} — battery still low"
                : $"[ALERT] {notification.DeviceName} — low battery";
        }

        if (notification.AlertType.Equals("DeviceOffline", StringComparison.Ordinal))
        {
            return $"[ALERT] {notification.DeviceName} — device offline";
        }

        if (notification.AlertType.Contains("OutOfRange", StringComparison.Ordinal))
        {
            return isReminder
                ? $"[ALERT] {notification.DeviceName} — {label} still out of range"
                : $"[ALERT] {notification.DeviceName} — {label} out of range";
        }

        return $"[ALERT] {notification.DeviceName} — {label}";
    }

    public static string BuildHtmlBody(AlertNotificationRequest notification, string subject)
    {
        return $"""
            <h2>{subject}</h2>
            <p><strong>Device:</strong> {notification.DeviceName}</p>
            <p><strong>Zone:</strong> {notification.ZoneName}</p>
            <p><strong>Alert:</strong> {notification.Message}</p>
            <p><strong>Time (UTC):</strong> {notification.TriggeredAtUtc:yyyy-MM-dd HH:mm:ss}</p>
            <hr />
            <p style="color:#666;font-size:12px;">SmartMonitoring alert notification</p>
            """;
    }

    private static string GetMetricLabel(string alertType) => alertType switch
    {
        "TemperatureOutOfRange" or "TemperatureNormalized" => "Temperature",
        "HumidityOutOfRange" or "HumidityNormalized" => "Humidity",
        "Co2OutOfRange" or "Co2Normalized" => "CO₂",
        "LightLevelOutOfRange" or "LightLevelNormalized" => "Light level",
        "NoiseLevelOutOfRange" or "NoiseLevelNormalized" => "Noise level",
        "LowBattery" or "BatteryNormalized" => "Battery",
        "DeviceOffline" => "Device offline",
        _ => "Sensor reading",
    };
}
