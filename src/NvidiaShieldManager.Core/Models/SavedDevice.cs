namespace NvidiaShieldManager.Core.Models;

public class SavedDevice
{
    public required string IpAddress { get; set; }
    public string? DeviceName { get; set; }
    public DateTime LastConnected { get; set; }
}
