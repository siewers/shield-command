namespace ShieldCommander.Core.Models;

public sealed record ProcessSnapshot(Dictionary<int, RawProcessEntry> Processes, long TotalJiffies, long IdleJiffies);
