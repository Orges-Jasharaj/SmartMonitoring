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
    public const string HumidityOutOfRange = "HumidityOutOfRange";
    public const string HumidityNormalized = "HumidityNormalized";
    public const string Co2OutOfRange = "Co2OutOfRange";
    public const string Co2Normalized = "Co2Normalized";
    public const string LowBattery = "LowBattery";
    public const string BatteryNormalized = "BatteryNormalized";
    public const string LightLevelOutOfRange = "LightLevelOutOfRange";
    public const string LightLevelNormalized = "LightLevelNormalized";
    public const string NoiseLevelOutOfRange = "NoiseLevelOutOfRange";
    public const string NoiseLevelNormalized = "NoiseLevelNormalized";
    public const string DeviceOffline = "DeviceOffline";
}
