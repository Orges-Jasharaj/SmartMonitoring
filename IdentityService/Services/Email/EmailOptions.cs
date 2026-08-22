namespace IdentityService.Services.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@smartmonitoring.local";
    public string FromName { get; set; } = "SmartMonitoring";
    public string ConfirmationBaseUrl { get; set; } = "http://localhost:8088/identity/api/authentication/confirm-email";
}
