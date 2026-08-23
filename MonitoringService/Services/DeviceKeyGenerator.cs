using System.Security.Cryptography;

namespace MonitoringService.Services;

public static class DeviceKeyGenerator
{
    public static string Generate()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
