using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShieldCommand.Core.Models;

public class ProcessInfo : INotifyPropertyChanged
{
    public int Pid { get; }

    private string _name;
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

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
        set { if (_memoryMb != value) { _memoryMb = value; OnPropertyChanged(); } }
    }

    public ProcessInfo(int pid, string name, double cpuPercent, double memoryMb)
    {
        Pid = pid;
        _name = name;
        _cpuPercent = cpuPercent;
        _memoryMb = memoryMb;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
