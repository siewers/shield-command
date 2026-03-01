using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShieldCommander.Core.Models;

public sealed class ProcessInfo : INotifyPropertyChanged
{
    public ProcessInfo(int pid, string name, string packageName, double cpuPercent, long memory, bool isUserApp,
        char stateChar)
    {
        Pid = pid;
        Name = name;
        PackageName = packageName;
        // Only strip path prefix for absolute paths (e.g. /system/bin/surfaceflinger → surfaceflinger)
        var slashIdx = packageName.StartsWith('/') ? packageName.LastIndexOf('/') : -1;
        FullName = slashIdx >= 0 ? packageName[(slashIdx + 1)..] : packageName;
        CpuPercent = cpuPercent;
        Memory = memory;
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
            _ => stateChar.ToString()
        };
    }

    public int Pid { get; }

    public string Name
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    /// Full cmdline value (package name or binary path, e.g. "com.google.android.youtube" or "/system/bin/surfaceflinger").
    /// Used by am force-stop for killing app processes.
    public string PackageName { get; }

    /// Human-readable name derived from cmdline — path prefix stripped
    /// (e.g. /system/bin/surfaceflinger → surfaceflinger). Package names pass through unchanged.
    public string FullName { get; }

    public double CpuPercent
    {
        get;
        set
        {
            if (Math.Abs(field - value) > 0.01)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public long Memory
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public string State { get; }

    public bool IsUserApp { get; }

    public string Kind => IsUserApp ? "User" : "System";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
