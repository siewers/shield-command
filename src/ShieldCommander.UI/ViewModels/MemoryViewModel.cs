using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ShieldCommander.Core.Models;
using SkiaSharp;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class MemoryViewModel : ViewModelBase, IActivityMonitor
{
    private TimeSpan _chartWindow;
    private TimeSpan _miniWindow;

    private static readonly Func<double, string> MbLabeler = v => v.ToString("F0") + " MB";

    [ObservableProperty] private string _memUsageText = "\u2014";
    [ObservableProperty] private double _memPhysical = double.NaN;
    [ObservableProperty] private double _memUsedDetail = double.NaN;
    [ObservableProperty] private double _memCached = double.NaN;
    [ObservableProperty] private string _swapUsedText = "\u2014";
    [ObservableProperty] private double _memFree = double.NaN;
    [ObservableProperty] private double _memBuffers = double.NaN;

    // Memory chart
    private readonly ObservableCollection<DateTimePoint> _memPoints = [];
    private readonly DateTimeAxis _memXAxis;
    private bool _memMaxSet;

    public ObservableCollection<ISeries> MemSeries { get; } = [];
    public Axis[] MemXAxes { get; }
    public Axis[] MemYAxes { get; } =
    [
        new() { MinLimit = 0, Labeler = MbLabeler, TextSize = 11 }
    ];
    public ObservableCollection<RectangularSection> MemSections { get; } = [];

    // Mini memory pressure chart
    private readonly ObservableCollection<DateTimePoint> _miniMemUsedPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniMemCachedPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniMemFreePoints = [];
    private readonly DateTimeAxis _miniMemXAxis;

    public ObservableCollection<ISeries> MemLoadSeries { get; } = [];
    public Axis[] MemLoadXAxes { get; }
    public Axis[] MemLoadYAxes { get; } =
    [
        new() { MinLimit = 0, ShowSeparatorLines = false, IsVisible = false }
    ];

    public MemoryViewModel(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;

        _memXAxis = ChartHelper.CreateTimeAxis();
        _miniMemXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };

        MemXAxes = [_memXAxis];
        MemLoadXAxes = [_miniMemXAxis];

        MemSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _memPoints,
            Fill = null,
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.LimeGreen, 1.5f),
            LineSmoothness = 0,
            Name = "Used MB",
        });

        MemLoadSeries.Add(new StackedAreaSeries<DateTimePoint>
        {
            Values = _miniMemUsedPoints,
            Fill = new SolidColorPaint(SKColors.OrangeRed.WithAlpha(180)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = null,
            LineSmoothness = 0,
            Name = "Used",
        });
        MemLoadSeries.Add(new StackedAreaSeries<DateTimePoint>
        {
            Values = _miniMemCachedPoints,
            Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(180)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = null,
            LineSmoothness = 0,
            Name = "Cached",
        });
        MemLoadSeries.Add(new StackedAreaSeries<DateTimePoint>
        {
            Values = _miniMemFreePoints,
            Fill = new SolidColorPaint(SKColors.LimeGreen.WithAlpha(120)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = null,
            LineSmoothness = 0,
            Name = "Free",
        });

        ChartHelper.UpdateAxisLimits(_memXAxis, DateTime.Now, _chartWindow, _miniWindow);
        ChartHelper.UpdateAxisLimits(_miniMemXAxis, DateTime.Now, _chartWindow, _miniWindow, mini: true);
    }

    public void Update(SystemSnapshot snapshot)
    {
        UpdateMemoryChart(snapshot.Memory);
    }

    public void Clear()
    {
        _memPoints.Clear();
        _memMaxSet = false;
        MemSections.Clear();
        _miniMemUsedPoints.Clear();
        _miniMemCachedPoints.Clear();
        _miniMemFreePoints.Clear();
        MemUsageText = "\u2014";
        MemPhysical = MemUsedDetail = MemCached = MemFree = MemBuffers = double.NaN;
        SwapUsedText = "\u2014";
    }

    public void SetWindows(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;
    }

    private void UpdateMemoryChart(MemorySnapshot mem)
    {
        if (mem.Total <= 0)
        {
            return;
        }

        const double toMb = 1024.0 * 1024.0;
        var usedMb = (mem.Total - mem.Available) / toMb;
        var totalMb = mem.Total / toMb;
        var now = DateTime.Now;

        _memPoints.Add(new DateTimePoint(now, usedMb));
        ChartHelper.TrimOldPoints(_memPoints, now, _chartWindow, _miniWindow);
        ChartHelper.UpdateAxisLimits(_memXAxis, now, _chartWindow, _miniWindow);

        if (!_memMaxSet)
        {
            MemYAxes[0].MaxLimit = Math.Ceiling(totalMb / 100.0) * 100;
            MemSections.Add(new RectangularSection
            {
                Yi = totalMb,
                Yj = totalMb,
                Stroke = new SolidColorPaint(SKColors.OrangeRed, 1f) { PathEffect = new DashEffect([4f, 4f]) },
                Label = $"Total: {totalMb:F0} MB",
                LabelPaint = new SolidColorPaint(SKColors.OrangeRed),
                LabelSize = 11,
            });
            _memMaxSet = true;
        }

        MemUsageText = $"{usedMb:F0} / {totalMb:F0} MB";

        var cachedMb = (mem.Cached + mem.Buffers) / toMb;
        var freeMb = mem.Free / toMb;
        var swapUsedMb = (mem.SwapTotal - mem.SwapFree) / toMb;
        var swapTotalMb = mem.SwapTotal / toMb;
        var appUsedMb = totalMb - freeMb - cachedMb;

        MemPhysical = totalMb;
        MemUsedDetail = usedMb;
        MemCached = cachedMb;
        MemFree = freeMb;
        MemBuffers = mem.Buffers / toMb;
        SwapUsedText = mem.SwapTotal > 0 ? $"{swapUsedMb:F0} / {swapTotalMb:F0} MB" : "N/A";

        var usedPct = appUsedMb / totalMb * 100.0;
        var cachedPct = cachedMb / totalMb * 100.0;
        var freePct = freeMb / totalMb * 100.0;
        _miniMemUsedPoints.Add(new DateTimePoint(now, usedPct));
        _miniMemCachedPoints.Add(new DateTimePoint(now, cachedPct));
        _miniMemFreePoints.Add(new DateTimePoint(now, freePct));
        ChartHelper.TrimOldPoints(_miniMemUsedPoints, now, _chartWindow, _miniWindow, mini: true);
        ChartHelper.TrimOldPoints(_miniMemCachedPoints, now, _chartWindow, _miniWindow, mini: true);
        ChartHelper.TrimOldPoints(_miniMemFreePoints, now, _chartWindow, _miniWindow, mini: true);
        ChartHelper.UpdateAxisLimits(_miniMemXAxis, now, _chartWindow, _miniWindow, mini: true);
        MemLoadYAxes[0].MaxLimit = 100;
    }
}
