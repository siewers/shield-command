using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NvidiaShieldManager.Core.Models;
using NvidiaShieldManager.Core.Services;

namespace NvidiaShieldManager.UI.ViewModels;

public partial class ProcessesViewModel : ViewModelBase
{
    private readonly AdbService _adbService;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _statusText = "Not monitoring";

    [ObservableProperty]
    private string _loadText = "";

    public ObservableCollection<ProcessInfo> Processes { get; } = [];

    public ProcessesViewModel(AdbService adbService)
    {
        _adbService = adbService;
    }

    public async Task StartAsync()
    {
        if (IsMonitoring) return;

        IsMonitoring = true;
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        StatusText = "Starting...";

        await PollAsync();
        StartMonitoringLoop();
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _timer?.Dispose();
        _timer = null;
        IsMonitoring = false;
        StatusText = "Monitoring stopped";
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await PollAsync();
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

    private async Task PollAsync()
    {
        var processes = await _adbService.GetProcessesAsync();

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Processes.Clear();
            foreach (var p in processes)
                Processes.Add(p);

            var totalCpu = processes.Sum(p => p.CpuPercent);
            LoadText = $"Total CPU: {totalCpu:F1}%";
            StatusText = $"{processes.Count} processes — {DateTime.Now:HH:mm:ss}";
        });
    }
}
