using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ShieldCommander.Core.Models;
using SkiaSharp;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class CpuViewModel : ViewModelBase, IActivityMonitor
{
    private static readonly Func<double, string> PercentLabeler = v => v.ToString("F0") + "%";

    private static readonly SKColor[] CoreColors =
    [
        SKColors.OrangeRed,
        SKColors.LimeGreen,
        SKColors.Gold,
        SKColors.MediumPurple,
        SKColors.Coral,
        SKColors.Cyan,
        SKColors.HotPink,
        SKColors.SteelBlue,
    ];

    private readonly Dictionary<string, (ObservableCollection<DateTimePoint> Points, long PrevActive, long PrevTotal, LineSeries<DateTimePoint> Series, ChartLegendItem Legend)> _coreState = new();
    private readonly DateTimeAxis _cpuXAxis;
    private readonly ObservableCollection<DateTimePoint> _miniSystemPoints = [];

    // Mini CPU load chart (stacked user + system)
    private readonly ObservableCollection<DateTimePoint> _miniUserPoints = [];
    private readonly DateTimeAxis _miniXAxis;
    private TimeSpan _chartWindow;

    [ObservableProperty]
    private double _cpuIdle = double.NaN;

    [ObservableProperty]
    private double _cpuSystem = double.NaN;

    [ObservableProperty]
    private double _cpuUsage = double.NaN;

    [ObservableProperty]
    private double _cpuUser = double.NaN;

    private TimeSpan _miniWindow;

    // CPU chart (per-core)
    private long _prevCpuActive, _prevCpuTotal, _prevCpuUser, _prevCpuSystem, _prevCpuIdle;

    [ObservableProperty]
    private int _processCount;

    [ObservableProperty]
    private int _threadCount;

    public CpuViewModel(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;

        _cpuXAxis = ChartHelper.CreateTimeAxis();
        _miniXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };

        CpuXAxes = [_cpuXAxis];
        CpuLoadXAxes = [_miniXAxis];

        CpuLoadSeries.Add(new StackedAreaSeries<DateTimePoint>
        {
            Values = _miniSystemPoints,
            Fill = new SolidColorPaint(SKColors.OrangeRed.WithAlpha(180)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.OrangeRed, 1f),
            LineSmoothness = 0,
            Name = "System",
        });

        CpuLoadSeries.Add(new StackedAreaSeries<DateTimePoint>
        {
            Values = _miniUserPoints,
            Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(180)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.DodgerBlue, 1f),
            LineSmoothness = 0,
            Name = "User",
        });

        ChartHelper.UpdateAxisLimits(_cpuXAxis, DateTime.Now, _chartWindow, _miniWindow);
        ChartHelper.UpdateAxisLimits(_miniXAxis, DateTime.Now, _chartWindow, _miniWindow, mini: true);
    }

    public ObservableCollection<ISeries> CpuSeries { get; } = [];

    public ObservableCollection<ChartLegendItem> CpuLegend { get; } = [];

    public Axis[] CpuXAxes { get; }

    public Axis[] CpuYAxes { get; } =
    [
        new() { MinLimit = 0, MaxLimit = 100, Labeler = PercentLabeler, TextSize = 11 },
    ];

    public ObservableCollection<ISeries> CpuLoadSeries { get; } = [];

    public Axis[] CpuLoadXAxes { get; }

    public Axis[] CpuLoadYAxes { get; } =
    [
        new() { MinLimit = 0, MaxLimit = 100, ShowSeparatorLines = false, IsVisible = false },
    ];

    public void Update(SystemSnapshot snapshot)
    {
        ProcessCount = snapshot.ProcessCount;
        ThreadCount = snapshot.ThreadCount;
        UpdateCpuChart(snapshot.Cpu);
    }

    public void Clear()
    {
        _prevCpuActive = _prevCpuTotal = _prevCpuUser = _prevCpuSystem = _prevCpuIdle = 0;
        foreach (var (_, state) in _coreState)
        {
            state.Points.Clear();
        }

        _coreState.Clear();
        CpuSeries.Clear();
        CpuLegend.Clear();
        _miniUserPoints.Clear();
        _miniSystemPoints.Clear();
        CpuUsage = CpuUser = CpuSystem = CpuIdle = double.NaN;
        ProcessCount = ThreadCount = 0;
    }

    public void SetWindows(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;
    }

    private void UpdateCpuChart(CpuSnapshot cpu)
    {
        var userJiffies = cpu.User + cpu.Nice;
        var systemJiffies = cpu.System + cpu.IoWait + cpu.Irq + cpu.SoftIrq + cpu.Steal;
        var active = userJiffies + systemJiffies;
        var total = active + cpu.Idle;
        var now = DateTime.Now;

        if (_prevCpuTotal > 0)
        {
            var deltaTotal = total - _prevCpuTotal;
            if (deltaTotal > 0)
            {
                var pct = (double)(active - _prevCpuActive) / deltaTotal * 100.0;
                var userPct = (double)(userJiffies - _prevCpuUser) / deltaTotal * 100.0;
                var sysPct = (double)(systemJiffies - _prevCpuSystem) / deltaTotal * 100.0;
                var idlePct = (double)(cpu.Idle - _prevCpuIdle) / deltaTotal * 100.0;

                CpuUsage = pct;
                CpuUser = userPct;
                CpuSystem = sysPct;
                CpuIdle = idlePct;

                _miniUserPoints.Add(new DateTimePoint(now, userPct));
                _miniSystemPoints.Add(new DateTimePoint(now, sysPct));
                ChartHelper.TrimOldPoints(_miniUserPoints, now, _chartWindow, _miniWindow, mini: true);
                ChartHelper.TrimOldPoints(_miniSystemPoints, now, _chartWindow, _miniWindow, mini: true);
                ChartHelper.UpdateAxisLimits(_miniXAxis, now, _chartWindow, _miniWindow, mini: true);
            }
        }

        _prevCpuActive = active;
        _prevCpuTotal = total;
        _prevCpuUser = userJiffies;
        _prevCpuSystem = systemJiffies;
        _prevCpuIdle = cpu.Idle;

        // Per-core usage
        foreach (var (name, coreActive, coreTotal) in cpu.Cores)
        {
            if (!_coreState.TryGetValue(name, out var state))
            {
                var points = new ObservableCollection<DateTimePoint>();
                var colorIndex = _coreState.Count % CoreColors.Length;
                var color = CoreColors[colorIndex];
                var series = new LineSeries<DateTimePoint>
                {
                    Values = points,
                    Fill = null,
                    GeometrySize = 0,
                    GeometryFill = null,
                    GeometryStroke = null,
                    Stroke = new SolidColorPaint(color, 1f),
                    LineSmoothness = 0,
                    Name = name,
                };

                CpuSeries.Add(series);
                var legend = new ChartLegendItem { Name = name, Color = ChartHelper.ToAvaloniaColor(color) };
                CpuLegend.Add(legend);
                state = (points, 0, 0, series, legend);
            }

            if (state.PrevTotal > 0)
            {
                var dActive = coreActive - state.PrevActive;
                var dTotal = coreTotal - state.PrevTotal;
                var corePct = dTotal > 0 ? (double)dActive / dTotal * 100.0 : 0;

                state.Points.Add(new DateTimePoint(now, corePct));
                ChartHelper.TrimOldPoints(state.Points, now, _chartWindow, _miniWindow);
                state.Legend.Value = corePct;
            }

            _coreState[name] = (state.Points, coreActive, coreTotal, state.Series, state.Legend);
        }

        ChartHelper.UpdateAxisLimits(_cpuXAxis, now, _chartWindow, _miniWindow);
    }
}
