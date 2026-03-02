using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class ProcessStatusQuery(int pid, string name) : IAdbQuery<ProcessDetails>
{
    public async Task<ProcessDetails> ExecuteAsync(AdbRunner runner)
    {
        var result = await runner.RunAdbAsync($"shell cat /proc/{pid}/status");

        if (!result.Success)
        {
            return new ProcessDetails(pid, name);
        }

        string? state = null, uid = null, threads = null, ppid = null;
        long? vmRss = null;

        foreach (var line in result.Output.Split('\n'))
        {
            var trimmed = line.Trim();

            if (state == null && trimmed.StartsWith("State:"))
            {
                state = trimmed["State:".Length..].Trim();
            }
            else if (uid == null && trimmed.StartsWith("Uid:"))
            {
                var parts = trimmed["Uid:".Length..].Trim().Split('\t', StringSplitOptions.RemoveEmptyEntries);
                uid = parts.Length > 0 ? parts[0] : null;
            }
            else if (threads == null && trimmed.StartsWith("Threads:"))
            {
                threads = trimmed["Threads:".Length..].Trim();
            }
            else if (vmRss == null && trimmed.StartsWith("VmRSS:"))
            {
                var rssText = trimmed["VmRSS:".Length..].Trim();
                var rssParts = rssText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (rssParts.Length >= 1 && long.TryParse(rssParts[0], out var kb))
                {
                    vmRss = kb * 1024L;
                }
            }
            else if (ppid == null && trimmed.StartsWith("PPid:"))
            {
                ppid = trimmed["PPid:".Length..].Trim();
            }
        }

        return new ProcessDetails(pid, name, state, uid, threads, vmRss, ppid);
    }
}
