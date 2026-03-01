using System.Collections.ObjectModel;
using Avalonia.Media;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

namespace ShieldCommander.UI.ViewModels;

public static class ChartHelper
{
    public static void TrimOldPoints(
        ObservableCollection<DateTimePoint> points,
        DateTime now,
        TimeSpan chartWindow,
        TimeSpan miniWindow,
        bool mini = false)
    {
        var window = mini ? miniWindow : chartWindow;
        var cutoff = now - window - TimeSpan.FromSeconds(window.TotalSeconds * 0.1);
        while (points.Count > 0 && points[0].DateTime < cutoff)
        {
            points.RemoveAt(0);
        }
    }

    public static void UpdateAxisLimits(
        DateTimeAxis axis,
        DateTime now,
        TimeSpan chartWindow,
        TimeSpan miniWindow,
        bool mini = false)
    {
        if (now == default)
        {
            now = DateTime.Now;
        }

        var window = mini ? miniWindow : chartWindow;
        axis.MinLimit = (now - window).Ticks;
        axis.MaxLimit = now.Ticks;
    }

    public static DateTimeAxis CreateTimeAxis() =>
        new(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };

    public static Color ToAvaloniaColor(SKColor c) =>
        Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
}
