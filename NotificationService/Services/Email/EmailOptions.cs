namespace NotificationService.Services.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; } = true;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "alerts@smartmonitoring.local";
    public string FromName { get; set; } = "SmartMonitoring Alerts";
}
