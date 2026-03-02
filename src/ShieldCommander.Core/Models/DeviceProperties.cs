namespace ShieldCommander.Core.Models;

public sealed record DeviceProperties(
    string? Model,
    string? Manufacturer,
    string? Architecture,
    string? AndroidVersion,
    string? ApiLevel,
    string? BuildId);
