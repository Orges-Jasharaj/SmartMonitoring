namespace MonitoringService.Data.Models;

public class Company
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<CompanyUser> Members { get; set; } = [];
    public ICollection<Device> Devices { get; set; } = [];
}
