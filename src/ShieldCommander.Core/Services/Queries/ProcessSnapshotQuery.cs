using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class ProcessSnapshotQuery : IAdbQuery<ProcessSnapshot>
{
    public async Task<ProcessSnapshot> ExecuteAsync(AdbRunner runner)
    {
        const string cmd = "cat /proc/stat; echo ---; cat /proc/[0-9]*/stat; echo ---; ls -ldn /proc/[0-9]*";

        var cmdlineTask = runner.RunAdbAsync("shell \"ps -A -o PID,ARGS\"", strictCheck: false);
        var shellTask = runner.RunShellWithFallbackAsync(cmd);

        var output = await shellTask;

        var procs = new Dictionary<int, RawProcessEntry>();
        var totalJiffies = 0L;
        var idleJiffies = 0L;

        if (string.IsNullOrWhiteSpace(output))
        {
            return new ProcessSnapshot(procs, totalJiffies, idleJiffies);
        }

        var procData = new Dictionary<int, (long Jiffies, string Name, long RssPages, char State)>();
        var section = 0;
        var uidByPid = new Dictionary<int, int>();

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed == "---")
            {
                section++;
                continue;
            }

            switch (section)
            {
                case 0:
                    ParseCpuStatLine(trimmed, ref totalJiffies, ref idleJiffies);
                    break;
                case 1:
                    ParseProcessStatLine(trimmed, procData);
                    break;
                case 2:
                    ParseProcDirLine(trimmed, uidByPid);
                    break;
            }
        }

        var cmdlineResult = await cmdlineTask;
        var cmdlineByPid = ParseCmdlineOutput(cmdlineResult);

        foreach (var (pid, (jiffies, name, rssPages, state)) in procData)
        {
            uidByPid.TryGetValue(pid, out var uid);
            cmdlineByPid.TryGetValue(pid, out var cmdline);
            procs[pid] = new RawProcessEntry(pid, jiffies, name, rssPages, uid, cmdline ?? name, state);
        }

        return new ProcessSnapshot(procs, totalJiffies, idleJiffies);
    }

    private static void ParseCpuStatLine(string trimmed, ref long totalJiffies, ref long idleJiffies)
    {
        if (!trimmed.StartsWith("cpu "))
        {
            return;
        }

        var fields = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 1; i < fields.Length; i++)
        {
            if (long.TryParse(fields[i], out var v))
            {
                totalJiffies += v;
            }
        }

        if (fields.Length > 4)
        {
            long.TryParse(fields[4], out idleJiffies);
        }
    }

    private static void ParseProcessStatLine(
        string trimmed,
        Dictionary<int, (long Jiffies, string Name, long RssPages, char State)> procData)
    {
        var commEnd = trimmed.LastIndexOf(')');
        if (commEnd < 0)
        {
            return;
        }

        var pidEnd = trimmed.IndexOf(' ');
        if (pidEnd < 0 || !int.TryParse(trimmed[..pidEnd], out var pid))
        {
            return;
        }

        var commStart = trimmed.IndexOf('(');
        var name = commStart >= 0 && commEnd > commStart
            ? trimmed[(commStart + 1)..commEnd]
            : pid.ToString();

        var afterComm = trimmed[(commEnd + 1)..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (afterComm.Length < 22)
        {
            return;
        }

        if (!long.TryParse(afterComm[11], out var utime) || !long.TryParse(afterComm[12], out var stime))
        {
            return;
        }

        long.TryParse(afterComm[21], out var rssPages);
        var state = afterComm[0].Length > 0 ? afterComm[0][0] : '?';
        procData[pid] = (utime + stime, name, rssPages, state);
    }

    private static void ParseProcDirLine(string trimmed, Dictionary<int, int> uidByPid)
    {
        var cols = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (cols.Length < 8)
        {
            return;
        }

        var path = cols[^1];
        var pidStr = path.StartsWith("/proc/") ? path["/proc/".Length..] : null;
        if (pidStr is not null && int.TryParse(pidStr, out var dirPid) && int.TryParse(cols[2], out var uid))
        {
            uidByPid[dirPid] = uid;
        }
    }

    private static Dictionary<int, string> ParseCmdlineOutput(AdbResult cmdlineResult)
    {
        var cmdlineByPid = new Dictionary<int, string>();
        if (cmdlineResult.Output is not { Length: > 0 })
        {
            return cmdlineByPid;
        }

        foreach (var line in cmdlineResult.Output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("PID"))
            {
                continue;
            }

            var spaceIdx = trimmed.IndexOf(' ');
            if (spaceIdx < 0 || !int.TryParse(trimmed[..spaceIdx], out var cmdPid))
            {
                continue;
            }

            var args = trimmed[(spaceIdx + 1)..].Trim();
            var firstArgEnd = args.IndexOf(' ');
            if (firstArgEnd > 0)
            {
                args = args[..firstArgEnd];
            }

            if (args.Length > 0)
            {
                cmdlineByPid[cmdPid] = args;
            }
        }

        return cmdlineByPid;
    }
}
