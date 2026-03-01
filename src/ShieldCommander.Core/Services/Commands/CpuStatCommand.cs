using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class CpuStatCommand : IAdbShellCommand<CpuSnapshot>
{
    public string Name => nameof(DynamicSections.Cpu);

    public string CommandText => "cat /proc/stat";

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
        Span<Range> vals = stackalloc Range[10];

        foreach (var statLine in output.EnumerateLines())
        {
            if (!statLine.StartsWith("cpu"))
            {
                continue;
            }

            var valCount = statLine.Split(vals, ' ', StringSplitOptions.RemoveEmptyEntries);
            if (valCount < 8)
            {
                continue;
            }

            long.TryParse(statLine[vals[1]], out var u);
            long.TryParse(statLine[vals[2]], out var n);
            long.TryParse(statLine[vals[3]], out var s);
            long.TryParse(statLine[vals[4]], out var id);
            long.TryParse(statLine[vals[5]], out var w);
            long.TryParse(statLine[vals[6]], out var q);
            long.TryParse(statLine[vals[7]], out var sq);
            long st = 0;
            if (valCount >= 9)
            {
                long.TryParse(statLine[vals[8]], out st);
            }

            var active = u + n + s + w + q + sq + st;
            var total = active + id;

            var label = statLine[vals[0]];
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
        }

        return new CpuSnapshot(user, nice, system, idle, ioWait, irq, softIrq, steal, cores);
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.Cpu = Parse(output);
}
