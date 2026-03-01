namespace ShieldCommander.Core.Models;

public sealed record ThermalSnapshot(
    string? Summary,
    List<(string Name, double Value)> Zones);
