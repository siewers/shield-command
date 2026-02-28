using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class ProcessesViewModel : ViewModelBase
{
    public AdbService AdbService { get; }
    private readonly ActivityMonitorViewModel _activityMonitor;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    // Previous snapshot for computing CPU% deltas
    private Dictionary<int, RawProcessEntry> _prevProcs = new();
    private long _prevTotalJiffies;
    private long _prevIdleJiffies;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _statusText = "Not monitoring";

    [ObservableProperty]
    private string _loadText = "";

    [ObservableProperty]
    private ProcessInfo? _selectedProcess;

    public ObservableCollection<ProcessInfo> Processes { get; } = [];

    public bool IsPollingSuspended { get; set; }

    [RelayCommand]
    private async Task KillProcessAsync()
    {
        if (SelectedProcess is not { } proc)
        {
            return;
        }

        var result = await AdbService.KillProcessAsync(proc.Pid, proc.PackageName);
        if (result.Success)
        {
            Processes.Remove(proc);
            StatusText = $"Killed {proc.Name} (PID {proc.Pid})";
        }
        else
        {
            StatusText = $"Failed to kill PID {proc.Pid}: {result.Error}";
        }
    }

    public ProcessesViewModel(AdbService adbService, ActivityMonitorViewModel activityMonitor)
    {
        AdbService = adbService;
        _activityMonitor = activityMonitor;

        _activityMonitor.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ActivityMonitorViewModel.SelectedRefreshRate) && IsMonitoring)
            {
                Stop();
                _ = StartAsync();
            }
        };
    }

    public async Task StartAsync()
    {
        if (IsMonitoring)
        {
            return;
        }

        IsMonitoring = true;
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(_activityMonitor.SelectedRefreshRate.Interval);
        StatusText = "Starting...";

        // Take two snapshots back-to-back so the first render has CPU% deltas
        if (_prevProcs.Count == 0)
        {
            var baseSnapshot = await AdbService.GetProcessSnapshotAsync();
            if (baseSnapshot.Processes.Count > 0)
            {
                _prevProcs = baseSnapshot.Processes;
                _prevTotalJiffies = baseSnapshot.TotalJiffies;
                _prevIdleJiffies = baseSnapshot.IdleJiffies;
                await Task.Delay(500);
            }
        }

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
                    if (!IsPollingSuspended)
                    {
                        await PollAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private async Task PollAsync()
    {
        var snapshot = await AdbService.GetProcessSnapshotAsync();
        if (snapshot.Processes.Count == 0)
        {
            return; // Bad read, skip this cycle
        }

        var processes = new List<ProcessInfo>();
        var deltaTotalJiffies = snapshot.TotalJiffies - _prevTotalJiffies;
        var hasPrev = deltaTotalJiffies > 0 && _prevProcs.Count > 0;

        foreach (var (pid, entry) in snapshot.Processes)
        {
            var cpuPct = 0.0;
            if (hasPrev && _prevProcs.TryGetValue(pid, out var prev))
            {
                var deltaProc = entry.Jiffies - prev.Jiffies;
                if (deltaProc > 0)
                {
                    cpuPct = (double)deltaProc / deltaTotalJiffies * 100.0;
                }
            }

            // Skip kernel threads (pid <= 2 or name starts with common kernel prefixes)
            if (pid <= 2)
            {
                continue;
            }

            var memMb = Math.Round(entry.RssPages * 4.0 / 1024.0, 1); // pages are 4KB on ARM
            // Android FIRST_APPLICATION_UID = 10000; UIDs >= 10000 are user-installed apps
            var isUserApp = entry.Uid >= 10000;
            processes.Add(new ProcessInfo(pid, entry.Name, entry.Cmdline, Math.Round(cpuPct, 1), memMb, isUserApp, entry.State));
        }

        // System-wide CPU% from /proc/stat idle delta (not the sum of per-process CPU%).
        // Per-process sum is always lower because kernel/irq/iowait time isn't tied to any PID.
        var systemCpuPct = 0.0;
        if (hasPrev)
        {
            var deltaIdle = snapshot.IdleJiffies - _prevIdleJiffies;
            systemCpuPct = (double)(deltaTotalJiffies - deltaIdle) / deltaTotalJiffies * 100.0;
        }

        _prevProcs = snapshot.Processes;
        _prevTotalJiffies = snapshot.TotalJiffies;
        _prevIdleJiffies = snapshot.IdleJiffies;

        var sorted = processes.OrderByDescending(p => p.CpuPercent).ToList();

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // In-place sync: update existing ProcessInfo objects via INotifyPropertyChanged
            // instead of Clear()+re-add. This avoids destroying items that the UI holds
            // references to (selection, context menus, scroll position).
            var newByPid = sorted.ToDictionary(p => p.Pid);

            // Remove processes that no longer exist
            for (var i = Processes.Count - 1; i >= 0; i--)
            {
                if (!newByPid.ContainsKey(Processes[i].Pid))
                {
                    Processes.RemoveAt(i);
                }
            }

            // Update existing and insert new processes in sorted order
            var existingByPid = new Dictionary<int, ProcessInfo>();
            foreach (var p in Processes)
            {
                existingByPid[p.Pid] = p;
            }

            for (var i = 0; i < sorted.Count; i++)
            {
                var incoming = sorted[i];
                if (i < Processes.Count && Processes[i].Pid == incoming.Pid)
                {
                    // Same position — update in place
                    CopyProcessFields(Processes[i], incoming);
                }
                else if (existingByPid.TryGetValue(incoming.Pid, out var existing))
                {
                    // Exists but wrong position — move it
                    var oldIndex = Processes.IndexOf(existing);
                    if (oldIndex >= 0 && oldIndex != i)
                    {
                        Processes.Move(oldIndex, i);
                    }

                    CopyProcessFields(existing, incoming);
                }
                else
                {
                    // New process
                    Processes.Insert(i, incoming);
                    existingByPid[incoming.Pid] = incoming;
                }
            }

            LoadText = $"Total CPU: {systemCpuPct:F1}%";
            StatusText = $"{sorted.Count} processes";
        });
    }

    private static void CopyProcessFields(ProcessInfo target, ProcessInfo source)
    {
        target.Name = source.Name;
        target.CpuPercent = source.CpuPercent;
        target.MemoryMb = source.MemoryMb;
    }
}
