using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShieldCommander.Core.Models;

public sealed class ProcessInfo : INotifyPropertyChanged
{
    public int Pid { get; }

    private string _name;
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    /// Full cmdline value (package name or binary path, e.g. "com.google.android.youtube" or "/system/bin/surfaceflinger").
    /// Used by am force-stop for killing app processes.
    public string PackageName { get; }

    /// Human-readable name derived from cmdline — path prefix stripped
    /// (e.g. /system/bin/surfaceflinger → surfaceflinger). Package names pass through unchanged.
    public string FullName { get; }

    private double _cpuPercent;
    public double CpuPercent
    {
        get => _cpuPercent;
        set { if (_cpuPercent != value) { _cpuPercent = value; OnPropertyChanged(); } }
    }

    private double _memoryMb;
    public double MemoryMb
    {
        get => _memoryMb;
        set
        {
            if (_memoryMb != value)
            {
                _memoryMb = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MemoryDisplay));
            }
        }
    }

    public string MemoryDisplay => _memoryMb > 0 ? $"{_memoryMb:F1} MB" : "\u2014";

    public string State { get; }

    public bool IsUserApp { get; }

    public string Kind => IsUserApp ? "User" : "System";

    public ProcessInfo(int pid, string name, string packageName, double cpuPercent, double memoryMb, bool isUserApp, char stateChar)
    {
        Pid = pid;
        _name = name;
        PackageName = packageName;
        // Only strip path prefix for absolute paths (e.g. /system/bin/surfaceflinger → surfaceflinger)
        var slashIdx = packageName.StartsWith('/') ? packageName.LastIndexOf('/') : -1;
        FullName = slashIdx >= 0 ? packageName[(slashIdx + 1)..] : packageName;
        _cpuPercent = cpuPercent;
        _memoryMb = memoryMb;
        IsUserApp = isUserApp;
        State = stateChar switch
        {
            'R' => "Running",
            'S' => "Sleeping",
            'D' => "Disk Sleep",
            'Z' => "Zombie",
            'T' => "Stopped",
            't' => "Traced",
            'X' => "Dead",
            _ => stateChar.ToString(),
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
