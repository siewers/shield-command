using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ShieldCommander.Core.Models;
using SkiaSharp;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class NetworkViewModel : ViewModelBase, IActivityMonitor
{
    private static readonly Func<double, string> KbsLabeler = v => v.ToString("F0") + " KB/s";

    private readonly ObservableCollection<DateTimePoint> _miniNetInPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniNetOutPoints = [];
    private readonly DateTimeAxis _miniNetXAxis;
    private readonly ObservableCollection<DateTimePoint> _netInPoints = [];
    private readonly ObservableCollection<DateTimePoint> _netOutPoints = [];
    private readonly DateTimeAxis _netXAxis;
    private TimeSpan _chartWindow;
    private TimeSpan _miniWindow;

    [ObservableProperty]
    private long _netDataIn;

    [ObservableProperty]
    private long _netDataOut;

    [ObservableProperty]
    private long _netInSpeed;

    [ObservableProperty]
    private long _netOutSpeed;

    [ObservableProperty]
    private long _netPacketsIn;

    [ObservableProperty]
    private long _netPacketsInPerSec;

    [ObservableProperty]
    private long _netPacketsOut;

    [ObservableProperty]
    private long _netPacketsOutPerSec;

    private long _prevNetBytesIn, _prevNetBytesOut;
    private long _prevNetPacketsIn, _prevNetPacketsOut;
    private DateTime _prevNetTime;

    public NetworkViewModel(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;

        _netXAxis = ChartHelper.CreateTimeAxis();
        _miniNetXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };

        NetXAxes = [_netXAxis];
        NetLoadXAxes = [_miniNetXAxis];

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

        NetLegend.Add(new ChartLegendItem { Name = "In", Color = ChartHelper.ToAvaloniaColor(SKColors.DodgerBlue) });
        NetLegend.Add(new ChartLegendItem { Name = "Out", Color = ChartHelper.ToAvaloniaColor(SKColors.OrangeRed) });

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

        ChartHelper.UpdateAxisLimits(_netXAxis, DateTime.Now, _chartWindow, _miniWindow);
        ChartHelper.UpdateAxisLimits(_miniNetXAxis, DateTime.Now, _chartWindow, _miniWindow, mini: true);
    }

    public ObservableCollection<ISeries> NetSeries { get; } = [];

    public ObservableCollection<ChartLegendItem> NetLegend { get; } = [];

    public Axis[] NetXAxes { get; }

    public Axis[] NetYAxes { get; } =
    [
        new() { MinLimit = 0, Labeler = KbsLabeler, TextSize = 11 },
    ];

    public ObservableCollection<ISeries> NetLoadSeries { get; } = [];

    public Axis[] NetLoadXAxes { get; }

    public Axis[] NetLoadYAxes { get; } =
    [
        new() { ShowSeparatorLines = false, IsVisible = false },
    ];

    public RectangularSection[] NetLoadSections { get; } =
    [
        new() { Yi = 0, Yj = 0, Stroke = new SolidColorPaint(SKColors.Gray.WithAlpha(100), 1f) },
    ];

    public void Update(SystemSnapshot snapshot)
    {
        UpdateNetworkChart(snapshot.Network);
    }

    public void Clear()
    {
        _prevNetBytesIn = _prevNetBytesOut = 0;
        _prevNetPacketsIn = _prevNetPacketsOut = 0;
        _prevNetTime = default;
        _netInPoints.Clear();
        _netOutPoints.Clear();
        _miniNetInPoints.Clear();
        _miniNetOutPoints.Clear();
        NetInSpeed = NetOutSpeed = NetPacketsInPerSec = NetPacketsOutPerSec = 0;
        NetPacketsIn = NetPacketsOut = NetDataIn = NetDataOut = 0;
    }

    public void SetWindows(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;
    }

    private void UpdateNetworkChart(NetworkSnapshot net)
    {
        var now = DateTime.Now;

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
                ChartHelper.TrimOldPoints(_netInPoints, now, _chartWindow, _miniWindow);
                ChartHelper.TrimOldPoints(_netOutPoints, now, _chartWindow, _miniWindow);

                _miniNetInPoints.Add(new DateTimePoint(now, inKbPerSec));
                _miniNetOutPoints.Add(new DateTimePoint(now, -outKbPerSec));
                ChartHelper.TrimOldPoints(_miniNetInPoints, now, _chartWindow, _miniWindow, mini: true);
                ChartHelper.TrimOldPoints(_miniNetOutPoints, now, _chartWindow, _miniWindow, mini: true);
                ChartHelper.UpdateAxisLimits(_miniNetXAxis, now, _chartWindow, _miniWindow, mini: true);

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

        ChartHelper.UpdateAxisLimits(_netXAxis, now, _chartWindow, _miniWindow);
    }
}
