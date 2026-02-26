namespace NvidiaShieldManager.Core.Models;

public record ShieldDevice(string IpAddress, string? DeviceName = null, bool IsConnected = false);
