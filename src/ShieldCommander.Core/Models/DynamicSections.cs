using ShieldCommander.Core.Services;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Models;

internal sealed class DynamicSections
{
    public MemoryInfo Memory { get; internal set; } = null!;

    public DiskFreeInfo? DiskFree { get; internal set; }

    public UptimeInfo? Uptime { get; internal set; }

    public ThermalSnapshot Thermal { get; internal set; } = null!;

    public CpuSnapshot Cpu { get; internal set; } = null!;

    public NetworkSnapshot Network { get; internal set; } = null!;

    public DiskSnapshot Disk { get; internal set; } = null!;

    internal static AdbCommandCollection CreateCommands()
    {
        var commands = new AdbCommandCollection
        {
            new MemInfoQuery(),
            new DiskFreeQuery(),
            new UptimeQuery(),
            new ThermalQuery(),
            new CpuStatQuery(),
            new NetDevQuery(),
            new DiskStatsQuery(),
        };

        return commands;
    }
}
