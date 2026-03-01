namespace ShieldCommander.Core.Models;

public sealed record NetworkSnapshot(
    long BytesIn,
    long BytesOut,
    long PacketsIn,
    long PacketsOut);
