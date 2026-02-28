namespace ShieldCommand.Core.Models;

public sealed record RawProcessEntry(int Pid, long Jiffies, string Name, long RssPages, int Uid, string Cmdline);
