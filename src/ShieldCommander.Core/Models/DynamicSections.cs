using ShieldCommander.Core.Services;
using ShieldCommander.Core.Services.Commands;

namespace ShieldCommander.Core.Models;

internal sealed class DynamicSections
{
    public MemoryInfo Memory { get; internal set; } = default!;

    public DiskFreeInfo? DiskFree { get; internal set; }

    public string? Uptime { get; internal set; }

    public ThermalSnapshot Thermal { get; internal set; } = null!;

    public CpuSnapshot Cpu { get; internal set; } = null!;

    public int ThreadCount { get; internal set; }

    public int ProcessCount { get; internal set; }

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
            new LoadAvgCommand(),
            new ProcessCountCommand(),
            new NetDevCommand(),
            new DiskStatsCommand(),
        };

        return commands;
    }
}
