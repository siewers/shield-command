namespace ShieldCommander.Core.Services;

public static class AppSettingsAccessor
{
    public static SettingsService Settings { get; set; } = new();
}
