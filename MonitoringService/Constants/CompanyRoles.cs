namespace MonitoringService.Constants;

public static class CompanyRoles
{
    public const string CompanyAdmin = "CompanyAdmin";
    public const string CompanyViewer = "CompanyViewer";

    public static readonly string[] All = [CompanyAdmin, CompanyViewer];
}

public static class SystemRoles
{
    public const string Admin = "Admin";
}

public static class AlertTypes
{
    public const string TemperatureOutOfRange = "TemperatureOutOfRange";
    public const string TemperatureNormalized = "TemperatureNormalized";
    public const string DeviceOffline = "DeviceOffline";
}
