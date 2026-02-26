namespace NvidiaShieldManager.Core.Models;

public class DeviceInfo
{
    // Static
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? Architecture { get; set; }
    public string? AndroidVersion { get; set; }
    public string? ApiLevel { get; set; }
    public string? BuildId { get; set; }

    // Dynamic — Memory
    public string? TotalRam { get; set; }
    public string? AvailableRam { get; set; }
    public long MemTotalKb { get; set; }
    public long MemAvailableKb { get; set; }
    public long MemFreeKb { get; set; }
    public long MemBuffersKb { get; set; }
    public long MemCachedKb { get; set; }
    public long SwapTotalKb { get; set; }
    public long SwapFreeKb { get; set; }
    public string? StorageTotal { get; set; }
    public string? StorageUsed { get; set; }
    public string? StorageAvailable { get; set; }
    public string? Uptime { get; set; }
    public string? Temperature { get; set; }
    public double? TemperatureValue { get; set; }
    public List<(string Name, double Value)> Temperatures { get; set; } = [];
    public string? FanState { get; set; }

    // CPU jiffies from /proc/stat (aggregate)
    public long CpuUser { get; set; }
    public long CpuNice { get; set; }
    public long CpuSystem { get; set; }
    public long CpuIdle { get; set; }
    public long CpuIoWait { get; set; }
    public long CpuIrq { get; set; }
    public long CpuSoftIrq { get; set; }
    public long CpuSteal { get; set; }

    // Per-core CPU jiffies (core index → active, total)
    public List<(string Name, long Active, long Total)> CpuCores { get; set; } = [];

    // Process / thread counts
    public int ProcessCount { get; set; }
    public int ThreadCount { get; set; }

    // Disk I/O from /proc/vmstat (KB paged in/out) + dumpsys diskstats
    public long DiskKbRead { get; set; }    // pgpgin
    public long DiskKbWritten { get; set; } // pgpgout
    public int DiskWriteLatencyMs { get; set; }
    public double DiskWriteSpeedKbps { get; set; }

    // Network I/O from /proc/net/dev (bytes)
    public long NetBytesIn { get; set; }
    public long NetBytesOut { get; set; }
    public long NetPacketsIn { get; set; }
    public long NetPacketsOut { get; set; }
}
