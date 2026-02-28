namespace ShieldCommander.Core.Models;

public sealed class SavedDevice
{
    public required string IpAddress { get; set; }
    public string? DeviceName { get; set; }
    public DateTime LastConnected { get; set; }
    public bool AutoConnect { get; set; }
}
