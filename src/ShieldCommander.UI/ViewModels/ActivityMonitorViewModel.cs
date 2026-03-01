using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ShieldCommander.Core.Services;
using ShieldCommander.UI.Models;
using SkiaSharp;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class ChartLegendItem : ObservableObject
{
    public string Name { get; init; } = "";
    public Avalonia.Media.Color Color { get; init; }
    [ObservableProperty] private double _value = double.NaN;
}

public sealed partial class ActivityMonitorViewModel : ViewModelBase
{
    private readonly AdbService _adbService;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private TimeSpan _chartWindow = RefreshRate.Default.ChartWindow;
    private TimeSpan _miniWindow = RefreshRate.Default.MiniWindow;

    private static readonly Func<double, string> PercentLabeler = v => v.ToString("F0") + "%";
    private static readonly Func<double, string> MbLabeler = v => v.ToString("F0") + " MB";
    private static readonly Func<double, string> KbsLabeler = v => v.ToString("F0") + " KB/s";
    private static readonly Func<double, string> DegreeLabeler = v => v.ToString("F0") + "\u00b0C";

    private static readonly SKColor[] ZoneColors =
    [
        SKColors.OrangeRed,
        SKColors.DodgerBlue,
        SKColors.LimeGreen,
        SKColors.Gold,
        SKColors.MediumPurple,
        SKColors.Coral,
        SKColors.Cyan,
        SKColors.HotPink,
    ];

    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private bool _isMonitoring;
    [ObservableProperty] private string _selectedMetric = "CPU";

    // Refresh interval
    [ObservableProperty] private RefreshRate _selectedRefreshRate = RefreshRate.Default;

    partial void OnSelectedRefreshRateChanged(RefreshRate value)
    {
        _chartWindow = value.ChartWindow;
        _miniWindow = value.MiniWindow;

        if (!IsMonitoring)
        {
            return;
        }

        // Cancel old loop and start a new one with the new interval
        _cts?.Cancel();
        _cts?.Dispose();
        _timer?.Dispose();
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(value.Interval);
        StartMonitoringLoop();
    }

    // CPU
    [ObservableProperty] private double _cpuUsage = double.NaN;
    [ObservableProperty] private double _cpuUser = double.NaN;
    [ObservableProperty] private double _cpuSystem = double.NaN;
    [ObservableProperty] private double _cpuIdle = double.NaN;

    // Memory — composite strings kept as-is
    [ObservableProperty] private string _memUsageText = "\u2014";
    [ObservableProperty] private string? _temperature;

    // Thermals
    [ObservableProperty] private double _avgTemperature = double.NaN;
    [ObservableProperty] private double _minTemperature = double.NaN;
    [ObservableProperty] private double _maxTemperature = double.NaN;
    [ObservableProperty] private string _hottestZoneText = "\u2014";
    [ObservableProperty] private int _zoneCount;
    [ObservableProperty] private string? _fanState;

    [ObservableProperty] private int _processCount;
    [ObservableProperty] private int _threadCount;

    // Disk
    [ObservableProperty] private long _diskReadSpeed;
    [ObservableProperty] private long _diskWriteSpeed;
    [ObservableProperty] private long _diskDataRead;
    [ObservableProperty] private long _diskDataWritten;
    [ObservableProperty] private int _diskLatency;
    [ObservableProperty] private long _diskRecentWriteSpeed;

    // Network
    [ObservableProperty] private long _netInSpeed;
    [ObservableProperty] private long _netOutSpeed;
    [ObservableProperty] private long _netPacketsIn;
    [ObservableProperty] private long _netPacketsOut;
    [ObservableProperty] private long _netDataIn;
    [ObservableProperty] private long _netDataOut;
    [ObservableProperty] private long _netPacketsInPerSec;
    [ObservableProperty] private long _netPacketsOutPerSec;

    // CPU chart (per-core)
    private long _prevCpuActive, _prevCpuTotal, _prevCpuUser, _prevCpuSystem, _prevCpuIdle;
    private readonly Dictionary<string, (ObservableCollection<DateTimePoint> Points, long PrevActive, long PrevTotal, LineSeries<DateTimePoint> Series, ChartLegendItem Legend)> _coreState = new();
    private readonly DateTimeAxis _cpuXAxis;

    public ObservableCollection<ISeries> CpuSeries { get; } = [];
    public ObservableCollection<ChartLegendItem> CpuLegend { get; } = [];
    public Axis[] CpuXAxes { get; }
    public Axis[] CpuYAxes { get; } =
    [
        new Axis { MinLimit = 0, MaxLimit = 100, Labeler = PercentLabeler, TextSize = 11 }
    ];

    // Mini CPU load chart (stacked user + system)
    private readonly ObservableCollection<DateTimePoint> _miniUserPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniSystemPoints = [];
    private readonly DateTimeAxis _miniXAxis;

    public ObservableCollection<ISeries> CpuLoadSeries { get; } = [];
    public Axis[] CpuLoadXAxes { get; }
    public Axis[] CpuLoadYAxes { get; } =
    [
        new Axis { MinLimit = 0, MaxLimit = 100, ShowSeparatorLines = false, IsVisible = false }
    ];

    // Memory chart
    private readonly ObservableCollection<DateTimePoint> _memPoints = [];
    private readonly DateTimeAxis _memXAxis;

    public ObservableCollection<ISeries> MemSeries { get; } = [];
    public Axis[] MemXAxes { get; }
    public Axis[] MemYAxes { get; } =
    [
        new Axis { MinLimit = 0, Labeler = MbLabeler, TextSize = 11 }
    ];

    private bool _memMaxSet;
    public ObservableCollection<RectangularSection> MemSections { get; } = [];

    // Mini memory pressure chart (stacked: used + cached)
    private readonly ObservableCollection<DateTimePoint> _miniMemUsedPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniMemCachedPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniMemFreePoints = [];
    private readonly DateTimeAxis _miniMemXAxis;

    public ObservableCollection<ISeries> MemLoadSeries { get; } = [];
    public Axis[] MemLoadXAxes { get; }
    public Axis[] MemLoadYAxes { get; } =
    [
        new Axis { MinLimit = 0, ShowSeparatorLines = false, IsVisible = false }
    ];

    // Memory stats
    [ObservableProperty] private double _memPhysical = double.NaN;
    [ObservableProperty] private double _memUsedDetail = double.NaN;
    [ObservableProperty] private double _memCached = double.NaN;
    [ObservableProperty] private string _swapUsedText = "\u2014";
    [ObservableProperty] private double _memFree = double.NaN;
    [ObservableProperty] private double _memBuffers = double.NaN;

    // Thermal chart — per-zone series
    private readonly Dictionary<string, (ObservableCollection<DateTimePoint> Points, ChartLegendItem Legend)> _zoneState = new();
    private readonly ObservableCollection<ISeries> _thermalSeries = [];
    private readonly DateTimeAxis _thermalXAxis;

    public ObservableCollection<ISeries> ThermalSeries => _thermalSeries;
    public ObservableCollection<ChartLegendItem> ThermalLegend { get; } = [];
    public Axis[] ThermalXAxes { get; }
    public Axis[] ThermalYAxes { get; } =
    [
        new Axis { Labeler = DegreeLabeler, TextSize = 11 }
    ];

    // Mini thermal chart (avg + hottest zone trend)
    private readonly ObservableCollection<DateTimePoint> _miniThermalAvgPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniThermalMaxPoints = [];
    private readonly DateTimeAxis _miniThermalXAxis;

    public ObservableCollection<ISeries> ThermalLoadSeries { get; } = [];
    public Axis[] ThermalLoadXAxes { get; }
    public Axis[] ThermalLoadYAxes { get; } =
    [
        new Axis { ShowSeparatorLines = false, IsVisible = false }
    ];

    // Disk I/O chart (from /proc/vmstat pgpgin/pgpgout, normalized to bytes)
    private long _prevDiskBytesRead, _prevDiskBytesWritten;
    private DateTime _prevDiskTime;
    private readonly ObservableCollection<DateTimePoint> _diskReadPoints = [];
    private readonly ObservableCollection<DateTimePoint> _diskWritePoints = [];
    private readonly DateTimeAxis _diskXAxis;

    // Mini disk IO chart
    private readonly ObservableCollection<DateTimePoint> _miniDiskReadPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniDiskWritePoints = [];
    private readonly DateTimeAxis _miniDiskXAxis;

    public ObservableCollection<ISeries> DiskSeries { get; } = [];
    public ObservableCollection<ChartLegendItem> DiskLegend { get; } = [];
    public Axis[] DiskXAxes { get; }
    public Axis[] DiskYAxes { get; } =
    [
        new Axis { MinLimit = 0, Labeler = KbsLabeler, TextSize = 11 }
    ];

    public ObservableCollection<ISeries> DiskLoadSeries { get; } = [];
    public Axis[] DiskLoadXAxes { get; }
    public Axis[] DiskLoadYAxes { get; } =
    [
        new Axis { ShowSeparatorLines = false, IsVisible = false }
    ];
    public RectangularSection[] DiskLoadSections { get; } =
    [
        new() { Yi = 0, Yj = 0, Stroke = new SolidColorPaint(SKColors.Gray.WithAlpha(100), 1f) }
    ];

    // Network I/O chart
    private long _prevNetBytesIn, _prevNetBytesOut;
    private long _prevNetPacketsIn, _prevNetPacketsOut;
    private DateTime _prevNetTime;
    private readonly ObservableCollection<DateTimePoint> _netInPoints = [];
    private readonly ObservableCollection<DateTimePoint> _netOutPoints = [];
    private readonly DateTimeAxis _netXAxis;

    // Mini network chart
    private readonly ObservableCollection<DateTimePoint> _miniNetInPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniNetOutPoints = [];
    private readonly DateTimeAxis _miniNetXAxis;

    public ObservableCollection<ISeries> NetSeries { get; } = [];
    public ObservableCollection<ChartLegendItem> NetLegend { get; } = [];
    public Axis[] NetXAxes { get; }
    public Axis[] NetYAxes { get; } =
    [
        new Axis { MinLimit = 0, Labeler = KbsLabeler, TextSize = 11 }
    ];

    public ObservableCollection<ISeries> NetLoadSeries { get; } = [];
    public Axis[] NetLoadXAxes { get; }
    public Axis[] NetLoadYAxes { get; } =
    [
        new Axis { ShowSeparatorLines = false, IsVisible = false }
    ];
    public RectangularSection[] NetLoadSections { get; } =
    [
        new() { Yi = 0, Yj = 0, Stroke = new SolidColorPaint(SKColors.Gray.WithAlpha(100), 1f) }
    ];

    public ActivityMonitorViewModel(AdbService adbService)
    {
        _adbService = adbService;

        _cpuXAxis = CreateTimeAxis();
        _memXAxis = CreateTimeAxis();
        _thermalXAxis = CreateTimeAxis();
        _diskXAxis = CreateTimeAxis();
        _netXAxis = CreateTimeAxis();
        _miniXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };
        _miniDiskXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };
        _miniNetXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };
        _miniMemXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };
        _miniThermalXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };

        CpuXAxes = [_cpuXAxis];
        MemXAxes = [_memXAxis];
        ThermalXAxes = [_thermalXAxis];
        DiskXAxes = [_diskXAxis];
        NetXAxes = [_netXAxis];
        CpuLoadXAxes = [_miniXAxis];
        DiskLoadXAxes = [_miniDiskXAxis];
        NetLoadXAxes = [_miniNetXAxis];
        MemLoadXAxes = [_miniMemXAxis];
        ThermalLoadXAxes = [_miniThermalXAxis];

        // Mini stacked area chart for CPU load panel
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

        // Mini memory pressure chart (stacked bottom->top: used, cached, free)
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

        // Disk I/O series
        DiskSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _diskReadPoints,
            Fill = null,
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.DodgerBlue, 1.5f),
            LineSmoothness = 0,
            Name = "Read",
        });
        DiskSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _diskWritePoints,
            Fill = null,
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.OrangeRed, 1.5f),
            LineSmoothness = 0,
            Name = "Write",
        });
        DiskLegend.Add(new ChartLegendItem { Name = "Read", Color = ToAvaloniaColor(SKColors.DodgerBlue) });
        DiskLegend.Add(new ChartLegendItem { Name = "Write", Color = ToAvaloniaColor(SKColors.OrangeRed) });

        // Mini disk IO chart (mirrored: read up, write down)
        DiskLoadSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _miniDiskReadPoints,
            Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(120)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.DodgerBlue, 1f),
            LineSmoothness = 0,
            Name = "Read",
        });
        DiskLoadSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _miniDiskWritePoints,
            Fill = new SolidColorPaint(SKColors.OrangeRed.WithAlpha(120)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.OrangeRed, 1f),
            LineSmoothness = 0,
            Name = "Write",
        });

        // Network I/O series
        NetSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _netInPoints,
            Fill = null,
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.DodgerBlue, 1.5f),
            LineSmoothness = 0,
            Name = "In",
        });
        NetSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _netOutPoints,
            Fill = null,
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.OrangeRed, 1.5f),
            LineSmoothness = 0,
            Name = "Out",
        });
        NetLegend.Add(new ChartLegendItem { Name = "In", Color = ToAvaloniaColor(SKColors.DodgerBlue) });
        NetLegend.Add(new ChartLegendItem { Name = "Out", Color = ToAvaloniaColor(SKColors.OrangeRed) });

        // Mini network chart (mirrored: in up, out down)
        NetLoadSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _miniNetInPoints,
            Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(120)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.DodgerBlue, 1f),
            LineSmoothness = 0,
            Name = "In",
        });
        NetLoadSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _miniNetOutPoints,
            Fill = new SolidColorPaint(SKColors.OrangeRed.WithAlpha(120)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.OrangeRed, 1f),
            LineSmoothness = 0,
            Name = "Out",
        });

        // Mini thermal chart (avg + hottest zone)
        ThermalLoadSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _miniThermalAvgPoints,
            Fill = new SolidColorPaint(SKColors.LimeGreen.WithAlpha(120)),
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.LimeGreen, 1f),
            LineSmoothness = 0,
            Name = "Avg",
        });
        ThermalLoadSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = _miniThermalMaxPoints,
            Fill = null,
            GeometrySize = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.OrangeRed, 1f),
            LineSmoothness = 0,
            Name = "Hottest",
        });

        UpdateAllAxisLimits();
    }

    public async Task StartAsync()
    {
        if (IsMonitoring)
        {
            return;
        }

        IsMonitoring = true;
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(SelectedRefreshRate.Interval);
        StatusText = "Starting...";

        // Initial poll immediately
        await PollAsync();

        StartMonitoringLoop();
    }

    private void StartMonitoringLoop()
    {
        var timer = _timer;
        var cts = _cts;
        _ = Task.Run(async () =>
        {
            try
            {
                while (timer is not null && await timer.WaitForNextTickAsync(cts!.Token))
                {
                    await PollAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on stop or interval change
            }
        });
    }

    public void Stop()
    {
        StopMonitoring();
    }

    public void Clear()
    {
        // CPU — core series are device-specific so remove them entirely
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

        // Memory — keep series (static structure), clear data
        _memPoints.Clear();
        _memMaxSet = false;
        MemSections.Clear();
        _miniMemUsedPoints.Clear();
        _miniMemCachedPoints.Clear();
        _miniMemFreePoints.Clear();

        // Thermals — zone series are device-specific so remove them entirely
        foreach (var (_, state) in _zoneState)
        {
            state.Points.Clear();
        }

        _zoneState.Clear();
        _thermalSeries.Clear();
        ThermalLegend.Clear();
        _miniThermalAvgPoints.Clear();
        _miniThermalMaxPoints.Clear();

        // Disk — keep series (static structure), clear data
        _prevDiskBytesRead = _prevDiskBytesWritten = 0;
        _prevDiskTime = default;
        _diskReadPoints.Clear();
        _diskWritePoints.Clear();
        _miniDiskReadPoints.Clear();
        _miniDiskWritePoints.Clear();

        // Network — keep series (static structure), clear data
        _prevNetBytesIn = _prevNetBytesOut = 0;
        _prevNetPacketsIn = _prevNetPacketsOut = 0;
        _prevNetTime = default;
        _netInPoints.Clear();
        _netOutPoints.Clear();
        _miniNetInPoints.Clear();
        _miniNetOutPoints.Clear();

        // Numeric properties
        CpuUsage = CpuUser = CpuSystem = CpuIdle = double.NaN;
        MemUsageText = "\u2014";
        Temperature = null;
        AvgTemperature = MinTemperature = MaxTemperature = double.NaN;
        HottestZoneText = "\u2014";
        ZoneCount = 0;
        FanState = null;
        ProcessCount = ThreadCount = 0;
        DiskReadSpeed = DiskWriteSpeed = DiskRecentWriteSpeed = 0;
        DiskDataRead = DiskDataWritten = 0;
        DiskLatency = 0;
        NetInSpeed = NetOutSpeed = NetPacketsInPerSec = NetPacketsOutPerSec = 0;
        NetPacketsIn = NetPacketsOut = NetDataIn = NetDataOut = 0;
        MemPhysical = MemUsedDetail = MemCached = MemFree = MemBuffers = double.NaN;
        SwapUsedText = "\u2014";
        StatusText = string.Empty;
    }

    private async Task PollAsync()
    {
        var snapshot = await _adbService.GetSystemSnapshotAsync();

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Temperature = snapshot.Thermal.Summary;
            ProcessCount = snapshot.ProcessCount;
            ThreadCount = snapshot.ThreadCount;
            UpdateCpuChart(snapshot.Cpu);
            UpdateMemoryChart(snapshot.Memory);
            UpdateDiskChart(snapshot.Disk);
            UpdateNetworkChart(snapshot.Network);
            AddTemperaturePoints(snapshot.Thermal);
            StatusText = $"Monitoring \u2014 {DateTime.Now:HH:mm:ss}";
        });
    }

    private void StopMonitoring()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _timer?.Dispose();
        _timer = null;
        IsMonitoring = false;
        StatusText = "Monitoring stopped";
    }

    // --- CPU ---

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

    private void UpdateCpuChart(Core.Models.CpuSnapshot cpu)
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

                // Mini load chart
                _miniUserPoints.Add(new DateTimePoint(now, userPct));
                _miniSystemPoints.Add(new DateTimePoint(now, sysPct));
                TrimOldPoints(_miniUserPoints, now, mini: true);
                TrimOldPoints(_miniSystemPoints, now, mini: true);
                UpdateAxisLimits(_miniXAxis, mini: true, now);
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
                var legend = new ChartLegendItem { Name = name, Color = ToAvaloniaColor(color) };
                CpuLegend.Add(legend);
                state = (points, 0, 0, series, legend);
            }

            if (state.PrevTotal > 0)
            {
                var dActive = coreActive - state.PrevActive;
                var dTotal = coreTotal - state.PrevTotal;
                var corePct = dTotal > 0 ? (double)dActive / dTotal * 100.0 : 0;

                state.Points.Add(new DateTimePoint(now, corePct));
                TrimOldPoints(state.Points, now);
                state.Legend.Value = corePct;
            }

            _coreState[name] = (state.Points, coreActive, coreTotal, state.Series, state.Legend);
        }

        UpdateAxisLimits(_cpuXAxis, now: now);
    }

    // --- Memory ---

    private void UpdateMemoryChart(Core.Models.MemorySnapshot mem)
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
        TrimOldPoints(_memPoints, now);
        UpdateAxisLimits(_memXAxis, now: now);

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

        // Memory stats
        var cachedMb = (mem.Cached + mem.Buffers) / toMb;
        var freeMb = mem.Free / toMb;
        var swapUsedMb = (mem.SwapTotal - mem.SwapFree) / toMb;
        var swapTotalMb = mem.SwapTotal / toMb;
        // "Used" here = Total - Free - Cached - Buffers (actual app memory)
        var appUsedMb = totalMb - freeMb - cachedMb;

        MemPhysical = totalMb;
        MemUsedDetail = usedMb;
        MemCached = cachedMb;
        MemFree = freeMb;
        MemBuffers = mem.Buffers / toMb;
        SwapUsedText = mem.SwapTotal > 0 ? $"{swapUsedMb:F0} / {swapTotalMb:F0} MB" : "N/A";

        // Mini memory pressure chart (used pct + cached pct)
        var usedPct = appUsedMb / totalMb * 100.0;
        var cachedPct = cachedMb / totalMb * 100.0;
        var freePct = freeMb / totalMb * 100.0;
        _miniMemUsedPoints.Add(new DateTimePoint(now, usedPct));
        _miniMemCachedPoints.Add(new DateTimePoint(now, cachedPct));
        _miniMemFreePoints.Add(new DateTimePoint(now, freePct));
        TrimOldPoints(_miniMemUsedPoints, now, mini: true);
        TrimOldPoints(_miniMemCachedPoints, now, mini: true);
        TrimOldPoints(_miniMemFreePoints, now, mini: true);
        UpdateAxisLimits(_miniMemXAxis, mini: true, now);
        MemLoadYAxes[0].MaxLimit = 100;
    }

    // --- Disk I/O ---

    private void UpdateDiskChart(Core.Models.DiskSnapshot disk)
    {
        if (disk.BytesRead == 0 && disk.BytesWritten == 0)
        {
            return;
        }

        var now = DateTime.Now;

        // Cumulative totals
        DiskDataRead = disk.BytesRead;
        DiskDataWritten = disk.BytesWritten;
        DiskLatency = disk.WriteLatencyMs;
        DiskRecentWriteSpeed = disk.WriteSpeed;

        if (_prevDiskTime != default)
        {
            var elapsed = (now - _prevDiskTime).TotalSeconds;
            if (elapsed > 0)
            {
                var readBytesPerSec = (disk.BytesRead - _prevDiskBytesRead) / elapsed;
                var writeBytesPerSec = (disk.BytesWritten - _prevDiskBytesWritten) / elapsed;

                var readKbPerSec = readBytesPerSec / 1024.0;
                var writeKbPerSec = writeBytesPerSec / 1024.0;

                _diskReadPoints.Add(new DateTimePoint(now, readKbPerSec));
                _diskWritePoints.Add(new DateTimePoint(now, writeKbPerSec));
                TrimOldPoints(_diskReadPoints, now);
                TrimOldPoints(_diskWritePoints, now);

                _miniDiskReadPoints.Add(new DateTimePoint(now, readKbPerSec));
                _miniDiskWritePoints.Add(new DateTimePoint(now, -writeKbPerSec));
                TrimOldPoints(_miniDiskReadPoints, now, mini: true);
                TrimOldPoints(_miniDiskWritePoints, now, mini: true);
                UpdateAxisLimits(_miniDiskXAxis, mini: true, now);

                DiskReadSpeed = (long)readBytesPerSec;
                DiskWriteSpeed = (long)writeBytesPerSec;
                DiskLegend[0].Value = (long)readBytesPerSec;
                DiskLegend[1].Value = (long)writeBytesPerSec;
            }
        }

        _prevDiskBytesRead = disk.BytesRead;
        _prevDiskBytesWritten = disk.BytesWritten;
        _prevDiskTime = now;

        UpdateAxisLimits(_diskXAxis, now: now);
    }

    // --- Network I/O ---

    private void UpdateNetworkChart(Core.Models.NetworkSnapshot net)
    {
        var now = DateTime.Now;

        // Cumulative totals
        NetPacketsIn = net.PacketsIn;
        NetPacketsOut = net.PacketsOut;
        NetDataIn = net.BytesIn;
        NetDataOut = net.BytesOut;

        if (_prevNetTime != default)
        {
            var elapsed = (now - _prevNetTime).TotalSeconds;
            if (elapsed > 0)
            {
                var inBytesPerSec = (net.BytesIn - _prevNetBytesIn) / elapsed;
                var outBytesPerSec = (net.BytesOut - _prevNetBytesOut) / elapsed;
                var inKbPerSec = inBytesPerSec / 1024.0;
                var outKbPerSec = outBytesPerSec / 1024.0;
                var pktsInPerSec = (net.PacketsIn - _prevNetPacketsIn) / elapsed;
                var pktsOutPerSec = (net.PacketsOut - _prevNetPacketsOut) / elapsed;

                _netInPoints.Add(new DateTimePoint(now, inKbPerSec));
                _netOutPoints.Add(new DateTimePoint(now, outKbPerSec));
                TrimOldPoints(_netInPoints, now);
                TrimOldPoints(_netOutPoints, now);

                _miniNetInPoints.Add(new DateTimePoint(now, inKbPerSec));
                _miniNetOutPoints.Add(new DateTimePoint(now, -outKbPerSec));
                TrimOldPoints(_miniNetInPoints, now, mini: true);
                TrimOldPoints(_miniNetOutPoints, now, mini: true);
                UpdateAxisLimits(_miniNetXAxis, mini: true, now);

                NetInSpeed = (long)inBytesPerSec;
                NetOutSpeed = (long)outBytesPerSec;
                NetPacketsInPerSec = (long)pktsInPerSec;
                NetPacketsOutPerSec = (long)pktsOutPerSec;
                NetLegend[0].Value = (long)inBytesPerSec;
                NetLegend[1].Value = (long)outBytesPerSec;
            }
        }

        _prevNetBytesIn = net.BytesIn;
        _prevNetBytesOut = net.BytesOut;
        _prevNetPacketsIn = net.PacketsIn;
        _prevNetPacketsOut = net.PacketsOut;
        _prevNetTime = now;

        UpdateAxisLimits(_netXAxis, now: now);
    }

    // --- Thermals ---

    private void AddTemperaturePoints(Core.Models.ThermalSnapshot thermal)
    {
        var temperatures = thermal.Zones;
        if (temperatures.Count == 0)
        {
            return;
        }

        var now = DateTime.Now;

        foreach (var (name, value) in temperatures)
        {
            if (!_zoneState.TryGetValue(name, out var state))
            {
                var points = new ObservableCollection<DateTimePoint>();
                var colorIndex = _thermalSeries.Count % ZoneColors.Length;
                var color = ZoneColors[colorIndex];
                _thermalSeries.Add(new LineSeries<DateTimePoint>
                {
                    Values = points,
                    Fill = null,
                    GeometrySize = 0,
                    GeometryFill = null,
                    GeometryStroke = null,
                    Stroke = new SolidColorPaint(color, 1.5f),
                    LineSmoothness = 0,
                    Name = name,
                });
                var legend = new ChartLegendItem { Name = name, Color = ToAvaloniaColor(color) };
                ThermalLegend.Add(legend);
                state = (points, legend);
                _zoneState[name] = state;
            }

            state.Points.Add(new DateTimePoint(now, value));
            TrimOldPoints(state.Points, now);
            state.Legend.Value = value;
        }

        UpdateAxisLimits(_thermalXAxis, now: now);

        // Enforce a minimum 10 deg C Y-axis range so small fluctuations aren't exaggerated
        // Scan all visible data points so historical values aren't clipped
        if (_zoneState.Count > 0)
        {
            var min = double.MaxValue;
            var max = double.MinValue;
            foreach (var (_, state) in _zoneState)
            {
                foreach (var pt in state.Points)
                {
                    if (pt.Value is { } v)
                    {
                        if (v < min)
                        {
                            min = v;
                        }

                        if (v > max)
                        {
                            max = v;
                        }
                    }
                }
            }
            if (min <= max)
            {
                var range = max - min;
                const double minRange = 10.0;
                if (range < minRange)
                {
                    var mid = (min + max) / 2.0;
                    min = mid - minRange / 2.0;
                    max = mid + minRange / 2.0;
                }
                ThermalYAxes[0].MinLimit = Math.Floor(min);
                ThermalYAxes[0].MaxLimit = Math.Ceiling(max);
            }
        }

        // Stats
        var avg = temperatures.Average(t => t.Value);
        var currentMin = temperatures.Min(t => t.Value);
        var currentMax = temperatures.Max(t => t.Value);
        var hottest = temperatures.MaxBy(t => t.Value);

        AvgTemperature = avg;
        MinTemperature = currentMin;
        MaxTemperature = currentMax;
        HottestZoneText = $"{hottest.Name} ({hottest.Value:F1}\u00b0C)";
        ZoneCount = temperatures.Count;
        FanState = thermal.FanState;

        // Mini thermal chart
        _miniThermalAvgPoints.Add(new DateTimePoint(now, avg));
        _miniThermalMaxPoints.Add(new DateTimePoint(now, currentMax));
        TrimOldPoints(_miniThermalAvgPoints, now, mini: true);
        TrimOldPoints(_miniThermalMaxPoints, now, mini: true);
        UpdateAxisLimits(_miniThermalXAxis, mini: true, now);
    }

    // --- Helpers ---

    private static DateTimeAxis CreateTimeAxis() =>
        new(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };

    private void TrimOldPoints(ObservableCollection<DateTimePoint> points, DateTime now, bool mini = false)
    {
        var window = mini ? _miniWindow : _chartWindow;
        // Keep points slightly beyond the visible window so the left edge isn't clipped
        var cutoff = now - window - TimeSpan.FromSeconds(window.TotalSeconds * 0.1);
        while (points.Count > 0 && points[0].DateTime < cutoff)
        {
            points.RemoveAt(0);
        }
    }

    private void UpdateAxisLimits(DateTimeAxis axis, bool mini = false, DateTime now = default)
    {
        if (now == default)
        {
            now = DateTime.Now;
        }

        var window = mini ? _miniWindow : _chartWindow;
        axis.MinLimit = (now - window).Ticks;
        axis.MaxLimit = now.Ticks;
    }

    private void UpdateAllAxisLimits()
    {
        UpdateAxisLimits(_cpuXAxis);
        UpdateAxisLimits(_memXAxis);
        UpdateAxisLimits(_thermalXAxis);
        UpdateAxisLimits(_diskXAxis);
        UpdateAxisLimits(_netXAxis);
        UpdateAxisLimits(_miniXAxis, mini: true);
        UpdateAxisLimits(_miniMemXAxis, mini: true);
        UpdateAxisLimits(_miniDiskXAxis, mini: true);
        UpdateAxisLimits(_miniNetXAxis, mini: true);
        UpdateAxisLimits(_miniThermalXAxis, mini: true);
    }

    private static Avalonia.Media.Color ToAvaloniaColor(SKColor c) =>
        Avalonia.Media.Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
}
