using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class ProcessesViewModel : ViewModelBase
{
    private readonly ActivityMonitorOrchestrator _activityMonitor;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _loadText = "";

    private long _prevIdleJiffies;

    // Previous snapshot for computing CPU% deltas
    private Dictionary<int, RawProcessEntry> _prevProcs = new();
    private long _prevTotalJiffies;

    [ObservableProperty]
    private ProcessInfo? _selectedProcess;

    [ObservableProperty]
    private string _statusText = "Not monitoring";

    private PeriodicTimer? _timer;

    public ProcessesViewModel(AdbService adbService, ActivityMonitorOrchestrator activityMonitor)
    {
        AdbService = adbService;
        _activityMonitor = activityMonitor;

        _activityMonitor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(ActivityMonitorOrchestrator.SelectedRefreshRate) || !IsMonitoring)
            {
                return;
            }

            Stop();
            _ = StartAsync();
        };
    }

    public AdbService AdbService { get; }

    public ObservableCollection<ProcessInfo> Processes { get; } = [];

    public bool IsPollingSuspended { get; set; }

    [RelayCommand]
    private async Task TerminateProcessAsync()
    {
        if (SelectedProcess is not { } proc)
        {
            return;
        }

        var result = await AdbService.TerminateProcessAsync(proc.Pid, proc.PackageName);
        if (result.Success)
        {
            Processes.Remove(proc);
            StatusText = $"Terminated {proc.Name} (PID {proc.Pid})";
        }
        else
        {
            StatusText = $"Failed to terminate PID {proc.Pid}: {result.Error}";
        }
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

    public void Clear()
    {
        Processes.Clear();
        _prevProcs.Clear();
        _prevTotalJiffies = _prevIdleJiffies = 0;
        SelectedProcess = null;
        LoadText = "";
        StatusText = string.Empty;
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
            return;// Bad read, skip this cycle
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

            var memBytes = entry.RssPages * 4096L;// pages are 4KB on ARM
            // Android FIRST_APPLICATION_UID = 10000; UIDs >= 10000 are user-installed apps
            var isUserApp = entry.Uid >= 10000;
            processes.Add(new ProcessInfo(pid, entry.Name, entry.Cmdline, Math.Round(cpuPct, 1), memBytes, isUserApp, entry.State));
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

        Dispatcher.UIThread.Post(() =>
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
        target.Memory = source.Memory;
    }
}
