namespace ShieldCommander.UI.Models;

public sealed record RefreshRate(string Label, TimeSpan Interval, TimeSpan ChartWindow, TimeSpan MiniWindow)
{
    public static RefreshRate[] All { get; } =
    [
        new("Very often (1 sec)", TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(15)),
        new("Often (2 sec)", TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(20)),
        new("Normally (5 sec)", TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(30)),
        new("Seldom (10 sec)", TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1)),
        new("Very seldom (30 sec)", TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(3)),
    ];

    public static RefreshRate Default => All[2];// "Normally (5 sec)"

    public override string ToString() => Label;
}
