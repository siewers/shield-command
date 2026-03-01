using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class CpuStatCommand : IAdbShellCommand<CpuSnapshot>
{
    public string Name => nameof(DynamicSections.Cpu);

    public string CommandText => "cat /proc/stat; cat /proc/loadavg; ls /proc/";

    public CpuSnapshot Parse(ReadOnlySpan<char> output)
    {
        long user = 0;
        long nice = 0;
        long system = 0;
        long idle = 0;
        long ioWait = 0;
        long irq = 0;
        long softIrq = 0;
        long steal = 0;
        var cores = new List<(string Name, long Active, long Total)>();
        var threadCount = 0;
        var processCount = 0;
        var pastCpuLines = false;
        var foundLoadAvg = false;
        Span<Range> vals = stackalloc Range[10];
        Span<Range> fields = stackalloc Range[6];

        foreach (var line in output.EnumerateLines())
        {
            var trimmed = line.Trim();
            if (trimmed.IsEmpty)
            {
                continue;
            }

            if (trimmed.StartsWith("cpu"))
            {
                var valCount = trimmed.Split(vals, ' ', StringSplitOptions.RemoveEmptyEntries);
                if (valCount < 8)
                {
                    continue;
                }

                long.TryParse(trimmed[vals[1]], out var u);
                long.TryParse(trimmed[vals[2]], out var n);
                long.TryParse(trimmed[vals[3]], out var s);
                long.TryParse(trimmed[vals[4]], out var id);
                long.TryParse(trimmed[vals[5]], out var w);
                long.TryParse(trimmed[vals[6]], out var q);
                long.TryParse(trimmed[vals[7]], out var sq);
                long st = 0;
                if (valCount >= 9)
                {
                    long.TryParse(trimmed[vals[8]], out st);
                }

                var active = u + n + s + w + q + sq + st;
                var total = active + id;

                var label = trimmed[vals[0]];
                if (label is "cpu")
                {
                    user = u;
                    nice = n;
                    system = s;
                    idle = id;
                    ioWait = w;
                    irq = q;
                    softIrq = sq;
                    steal = st;
                }
                else
                {
                    cores.Add((label.ToString().ToUpperInvariant(), active, total));
                }

                continue;
            }

            if (!pastCpuLines)
            {
                pastCpuLines = true;
            }

            // Parse /proc/loadavg line for thread count
            if (!foundLoadAvg && pastCpuLines)
            {
                var fieldCount = trimmed.Split(fields, ' ');
                if (fieldCount >= 4)
                {
                    var runnable = trimmed[fields[3]];
                    var slashIdx = runnable.IndexOf('/');
                    if (slashIdx >= 0 && int.TryParse(runnable[(slashIdx + 1)..], out var threads))
                    {
                        threadCount = threads;
                        foundLoadAvg = true;
                        continue;
                    }
                }
            }

            // Count all-digit entries from ls /proc/ for process count
            if (foundLoadAvg && ParseHelper.IsAllDigits(trimmed))
            {
                processCount++;
            }
        }

        return new CpuSnapshot(user, nice, system, idle, ioWait, irq, softIrq, steal, cores, processCount, threadCount);
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.Cpu = Parse(output);
}
