namespace ShieldCommander.Core.Models;

public sealed record DiskSnapshot(
    long BytesRead,
    long BytesWritten,
    int WriteLatencyMs,
    long WriteSpeed);
