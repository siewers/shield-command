namespace ShieldCommander.Core.Models;

public sealed class DeviceInfo
{
    public string? Model { get; set; }

    public string? Manufacturer { get; set; }

    public string? Architecture { get; set; }

    public string? AndroidVersion { get; set; }

    public string? ApiLevel { get; set; }

    public string? BuildId { get; set; }

    public long? RamTotal { get; set; }

    public long? StorageTotal { get; set; }
}
