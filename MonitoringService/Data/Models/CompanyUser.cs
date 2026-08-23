namespace MonitoringService.Data.Models;

public class CompanyUser
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string UserId { get; set; }
    public required string Role { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
}
