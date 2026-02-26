namespace NvidiaShieldManager.Core.Models;

public record DiscoveredDevice(string IpAddress, string? DisplayName = null);
