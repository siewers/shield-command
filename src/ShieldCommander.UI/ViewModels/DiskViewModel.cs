using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ShieldCommander.Core.Models;
using SkiaSharp;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class DiskViewModel : ViewModelBase, IActivityMonitor
{
    private TimeSpan _chartWindow;
    private TimeSpan _miniWindow;

    private static readonly Func<double, string> KbsLabeler = v => v.ToString("F0") + " KB/s";

    [ObservableProperty] private long _diskReadSpeed;
    [ObservableProperty] private long _diskWriteSpeed;
    [ObservableProperty] private long _diskDataRead;
    [ObservableProperty] private long _diskDataWritten;
    [ObservableProperty] private int _diskLatency;
    [ObservableProperty] private long _diskRecentWriteSpeed;

    private long _prevDiskBytesRead, _prevDiskBytesWritten;
    private DateTime _prevDiskTime;
    private readonly ObservableCollection<DateTimePoint> _diskReadPoints = [];
    private readonly ObservableCollection<DateTimePoint> _diskWritePoints = [];
    private readonly DateTimeAxis _diskXAxis;

    private readonly ObservableCollection<DateTimePoint> _miniDiskReadPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniDiskWritePoints = [];
    private readonly DateTimeAxis _miniDiskXAxis;

    public ObservableCollection<ISeries> DiskSeries { get; } = [];
    public ObservableCollection<ChartLegendItem> DiskLegend { get; } = [];
    public Axis[] DiskXAxes { get; }
    public Axis[] DiskYAxes { get; } =
    [
        new() { MinLimit = 0, Labeler = KbsLabeler, TextSize = 11 }
    ];

    public ObservableCollection<ISeries> DiskLoadSeries { get; } = [];
    public Axis[] DiskLoadXAxes { get; }
    public Axis[] DiskLoadYAxes { get; } =
    [
        new() { ShowSeparatorLines = false, IsVisible = false }
    ];
    public RectangularSection[] DiskLoadSections { get; } =
    [
        new() { Yi = 0, Yj = 0, Stroke = new SolidColorPaint(SKColors.Gray.WithAlpha(100), 1f) }
    ];

    public DiskViewModel(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;

        _diskXAxis = ChartHelper.CreateTimeAxis();
        _miniDiskXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };

        DiskXAxes = [_diskXAxis];
        DiskLoadXAxes = [_miniDiskXAxis];

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
        DiskLegend.Add(new ChartLegendItem { Name = "Read", Color = ChartHelper.ToAvaloniaColor(SKColors.DodgerBlue) });
        DiskLegend.Add(new ChartLegendItem { Name = "Write", Color = ChartHelper.ToAvaloniaColor(SKColors.OrangeRed) });

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

        ChartHelper.UpdateAxisLimits(_diskXAxis, DateTime.Now, _chartWindow, _miniWindow);
        ChartHelper.UpdateAxisLimits(_miniDiskXAxis, DateTime.Now, _chartWindow, _miniWindow, mini: true);
    }

    public void Update(SystemSnapshot snapshot)
    {
        UpdateDiskChart(snapshot.Disk);
    }

    public void Clear()
    {
        _prevDiskBytesRead = _prevDiskBytesWritten = 0;
        _prevDiskTime = default;
        _diskReadPoints.Clear();
        _diskWritePoints.Clear();
        _miniDiskReadPoints.Clear();
        _miniDiskWritePoints.Clear();
        DiskReadSpeed = DiskWriteSpeed = DiskRecentWriteSpeed = 0;
        DiskDataRead = DiskDataWritten = 0;
        DiskLatency = 0;
    }

    public void SetWindows(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;
    }

    private void UpdateDiskChart(DiskSnapshot disk)
    {
        if (disk.BytesRead == 0 && disk.BytesWritten == 0)
        {
            return;
        }

        var now = DateTime.Now;

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
                ChartHelper.TrimOldPoints(_diskReadPoints, now, _chartWindow, _miniWindow);
                ChartHelper.TrimOldPoints(_diskWritePoints, now, _chartWindow, _miniWindow);

                _miniDiskReadPoints.Add(new DateTimePoint(now, readKbPerSec));
                _miniDiskWritePoints.Add(new DateTimePoint(now, -writeKbPerSec));
                ChartHelper.TrimOldPoints(_miniDiskReadPoints, now, _chartWindow, _miniWindow, mini: true);
                ChartHelper.TrimOldPoints(_miniDiskWritePoints, now, _chartWindow, _miniWindow, mini: true);
                ChartHelper.UpdateAxisLimits(_miniDiskXAxis, now, _chartWindow, _miniWindow, mini: true);

                DiskReadSpeed = (long)readBytesPerSec;
                DiskWriteSpeed = (long)writeBytesPerSec;
                DiskLegend[0].Value = (long)readBytesPerSec;
                DiskLegend[1].Value = (long)writeBytesPerSec;
            }
        }

        _prevDiskBytesRead = disk.BytesRead;
        _prevDiskBytesWritten = disk.BytesWritten;
        _prevDiskTime = now;

        ChartHelper.UpdateAxisLimits(_diskXAxis, now, _chartWindow, _miniWindow);
    }
}
