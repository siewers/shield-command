namespace ShieldCommander.Core.Models;

public sealed record SystemSnapshot(
    CpuSnapshot Cpu,
    MemorySnapshot Memory,
    DiskSnapshot Disk,
    NetworkSnapshot Network,
    ThermalSnapshot Thermal,
    int ProcessCount,
    int ThreadCount);
