namespace ShieldCommander.Core.Models;

public sealed record ProcessDetails(
    int Pid,
    string Name,
    string? State = null,
    string? Uid = null,
    string? Threads = null,
    string? VmRss = null,
    string? PPid = null,
    string? CpuGroup = null);
