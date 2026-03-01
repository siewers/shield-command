using ShieldCommander.Core.Services;
using ShieldCommander.Core.Services.Commands;

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
            new MemInfoCommand(),
            new DiskFreeCommand(),
            new UptimeCommand(),
            new ThermalCommand(),
            new CpuStatCommand(),
            new NetDevCommand(),
            new DiskStatsCommand(),
        };

        return commands;
    }
}
