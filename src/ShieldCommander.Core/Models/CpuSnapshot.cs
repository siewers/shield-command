namespace ShieldCommander.Core.Models;

public sealed record CpuSnapshot(
    long User,
    long Nice,
    long System,
    long Idle,
    long IoWait,
    long Irq,
    long SoftIrq,
    long Steal,
    List<(string Name, long Active, long Total)> Cores);
