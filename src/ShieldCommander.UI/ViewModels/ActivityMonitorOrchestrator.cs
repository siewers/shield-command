using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ShieldCommander.Core.Services;
using ShieldCommander.UI.Models;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class ActivityMonitorOrchestrator : ViewModelBase
{
    private readonly AdbService _adbService;
    private readonly IActivityMonitor[] _panels;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _selectedMetric = "CPU";

    [ObservableProperty]
    private RefreshRate _selectedRefreshRate = RefreshRate.Default;

    private PeriodicTimer? _timer;

    public ActivityMonitorOrchestrator(AdbService adbService)
    {
        _adbService = adbService;

        var chartWindow = RefreshRate.Default.ChartWindow;
        var miniWindow = RefreshRate.Default.MiniWindow;

        CpuVm = new CpuViewModel(chartWindow, miniWindow);
        MemoryVm = new MemoryViewModel(chartWindow, miniWindow);
        DiskVm = new DiskViewModel(chartWindow, miniWindow);
        NetworkVm = new NetworkViewModel(chartWindow, miniWindow);
        ThermalVm = new ThermalViewModel(chartWindow, miniWindow);

        _panels = [CpuVm, MemoryVm, DiskVm, NetworkVm, ThermalVm];
    }

    public CpuViewModel CpuVm { get; }

    public MemoryViewModel MemoryVm { get; }

    public DiskViewModel DiskVm { get; }

    public NetworkViewModel NetworkVm { get; }

    public ThermalViewModel ThermalVm { get; }

    partial void OnSelectedRefreshRateChanged(RefreshRate value)
    {
        var chartWindow = value.ChartWindow;
        var miniWindow = value.MiniWindow;

        foreach (var panel in _panels)
        {
            panel.SetWindows(chartWindow, miniWindow);
        }

        if (!IsMonitoring)
        {
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _timer?.Dispose();
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(value.Interval);
        StartMonitoringLoop();
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
            }
        });
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _timer?.Dispose();
        _timer = null;
        IsMonitoring = false;
    }

    public void Clear()
    {
        foreach (var panel in _panels)
        {
            panel.Clear();
        }
    }

    private async Task PollAsync()
    {
        var snapshot = await _adbService.GetSystemSnapshotAsync();

        Dispatcher.UIThread.Post(() =>
        {
            foreach (var panel in _panels)
            {
                panel.Update(snapshot);
            }
        });
    }
}
