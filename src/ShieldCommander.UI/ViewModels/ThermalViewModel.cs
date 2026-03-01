using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ShieldCommander.Core.Models;
using SkiaSharp;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class ThermalViewModel : ViewModelBase, IActivityMonitor
{
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

    // Mini thermal chart
    private readonly ObservableCollection<DateTimePoint> _miniThermalAvgPoints = [];
    private readonly ObservableCollection<DateTimePoint> _miniThermalMaxPoints = [];
    private readonly DateTimeAxis _miniThermalXAxis;
    private readonly DateTimeAxis _thermalXAxis;

    // Thermal chart — per-zone series
    private readonly Dictionary<string, (ObservableCollection<DateTimePoint> Points, ChartLegendItem Legend)> _zoneState = new();

    [ObservableProperty]
    private double _avgTemperature = double.NaN;

    private TimeSpan _chartWindow;

    [ObservableProperty]
    private string? _fanState;

    [ObservableProperty]
    private string _hottestZoneText = "\u2014";

    [ObservableProperty]
    private double _maxTemperature = double.NaN;

    private TimeSpan _miniWindow;

    [ObservableProperty]
    private double _minTemperature = double.NaN;

    [ObservableProperty]
    private string? _temperature;

    [ObservableProperty]
    private int _zoneCount;

    public ThermalViewModel(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;

        _thermalXAxis = ChartHelper.CreateTimeAxis();
        _miniThermalXAxis = new DateTimeAxis(TimeSpan.FromSeconds(30), _ => "") { IsVisible = false };

        ThermalXAxes = [_thermalXAxis];
        ThermalLoadXAxes = [_miniThermalXAxis];

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

        ChartHelper.UpdateAxisLimits(_thermalXAxis, DateTime.Now, _chartWindow, _miniWindow);
        ChartHelper.UpdateAxisLimits(_miniThermalXAxis, DateTime.Now, _chartWindow, _miniWindow, mini: true);
    }

    public ObservableCollection<ISeries> ThermalSeries { get; } = [];

    public ObservableCollection<ChartLegendItem> ThermalLegend { get; } = [];

    public Axis[] ThermalXAxes { get; }

    public Axis[] ThermalYAxes { get; } =
    [
        new() { Labeler = DegreeLabeler, TextSize = 11 },
    ];

    public ObservableCollection<ISeries> ThermalLoadSeries { get; } = [];

    public Axis[] ThermalLoadXAxes { get; }

    public Axis[] ThermalLoadYAxes { get; } =
    [
        new() { ShowSeparatorLines = false, IsVisible = false },
    ];

    public void Update(SystemSnapshot snapshot)
    {
        Temperature = snapshot.Thermal.Summary;
        AddTemperaturePoints(snapshot.Thermal);
    }

    public void Clear()
    {
        foreach (var (_, state) in _zoneState)
        {
            state.Points.Clear();
        }

        _zoneState.Clear();
        ThermalSeries.Clear();
        ThermalLegend.Clear();
        _miniThermalAvgPoints.Clear();
        _miniThermalMaxPoints.Clear();
        Temperature = null;
        AvgTemperature = MinTemperature = MaxTemperature = double.NaN;
        HottestZoneText = "\u2014";
        ZoneCount = 0;
        FanState = null;
    }

    public void SetWindows(TimeSpan chartWindow, TimeSpan miniWindow)
    {
        _chartWindow = chartWindow;
        _miniWindow = miniWindow;
    }

    private void AddTemperaturePoints(ThermalSnapshot thermal)
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
                var colorIndex = ThermalSeries.Count % ZoneColors.Length;
                var color = ZoneColors[colorIndex];
                ThermalSeries.Add(new LineSeries<DateTimePoint>
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

                var legend = new ChartLegendItem { Name = name, Color = ChartHelper.ToAvaloniaColor(color) };
                ThermalLegend.Add(legend);
                state = (points, legend);
                _zoneState[name] = state;
            }

            state.Points.Add(new DateTimePoint(now, value));
            ChartHelper.TrimOldPoints(state.Points, now, _chartWindow, _miniWindow);
            state.Legend.Value = value;
        }

        ChartHelper.UpdateAxisLimits(_thermalXAxis, now, _chartWindow, _miniWindow);

        // Enforce a minimum 10 deg C Y-axis range
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
        ChartHelper.TrimOldPoints(_miniThermalAvgPoints, now, _chartWindow, _miniWindow, mini: true);
        ChartHelper.TrimOldPoints(_miniThermalMaxPoints, now, _chartWindow, _miniWindow, mini: true);
        ChartHelper.UpdateAxisLimits(_miniThermalXAxis, now, _chartWindow, _miniWindow, mini: true);
    }
}
