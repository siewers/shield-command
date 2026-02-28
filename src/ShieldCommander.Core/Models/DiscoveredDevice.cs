namespace ShieldCommander.Core.Models;

public sealed record DiscoveredDevice(string IpAddress, string? DisplayName = null);
