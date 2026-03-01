using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class CpuStatCommand : IAdbShellCommand<CpuSnapshot>
{
    public string Name => nameof(DynamicSections.Cpu);

    public string CommandText => "cat /proc/stat";

    public CpuSnapshot Parse(string output)
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

        foreach (var statLine in output.Split('\n'))
        {
            if (!statLine.StartsWith("cpu"))
            {
                continue;
            }

            var vals = statLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (vals.Length < 8)
            {
                continue;
            }

            long.TryParse(vals[1], out var u);
            long.TryParse(vals[2], out var n);
            long.TryParse(vals[3], out var s);
            long.TryParse(vals[4], out var id);
            long.TryParse(vals[5], out var w);
            long.TryParse(vals[6], out var q);
            long.TryParse(vals[7], out var sq);
            long st = 0;
            if (vals.Length >= 9)
            {
                long.TryParse(vals[8], out st);
            }

            var active = u + n + s + w + q + sq + st;
            var total = active + id;

            if (vals[0] == "cpu")
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
                cores.Add((vals[0].ToUpperInvariant(), active, total));
            }
        }

        return new CpuSnapshot(user, nice, system, idle, ioWait, irq, softIrq, steal, cores);
    }

    public void Apply(string output, DynamicSections target)
        => target.Cpu = Parse(output);
}
