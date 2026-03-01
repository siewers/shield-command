namespace ShieldCommander.Core.Models;

public sealed record MemorySnapshot(
    long Total,
    long Available,
    long Free,
    long Buffers,
    long Cached,
    long SwapTotal,
    long SwapFree);
