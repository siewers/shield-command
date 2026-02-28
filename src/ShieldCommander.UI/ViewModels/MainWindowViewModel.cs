using CommunityToolkit.Mvvm.ComponentModel;
using ShieldCommander.Core.Services;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly AdbService _adbService = new();
    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private bool _isDeviceConnected;

    [ObservableProperty]
    private string _windowTitle = "Shield Commander — Disconnected";

    [ObservableProperty]
    private string _connectionStatusText = "Disconnected";

    public DeviceViewModel DevicePage { get; }
    public AppsViewModel AppsPage { get; }
    public InstallViewModel InstallPage { get; }
    public SystemViewModel SystemPage { get; }
    public ActivityMonitorViewModel ActivityMonitorPage { get; }
    public ProcessesViewModel ProcessesPage { get; }

    public MainWindowViewModel()
    {
        DevicePage = new DeviceViewModel(_adbService);
        AppsPage = new AppsViewModel(_adbService);
        InstallPage = new InstallViewModel(_adbService);
        SystemPage = new SystemViewModel(_adbService);
        ActivityMonitorPage = new ActivityMonitorViewModel(_adbService);
        ProcessesPage = new ProcessesViewModel(_adbService, ActivityMonitorPage);
        _currentPage = SystemPage;

        DevicePage.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(DeviceViewModel.IsConnected))
            {
                return;
            }

            IsDeviceConnected = DevicePage.IsConnected;

            if (DevicePage.IsConnected)
            {
                var name = DevicePage.ConnectedDeviceName;
                var ip = DevicePage.IpAddress;
                WindowTitle = string.IsNullOrEmpty(name)
                    ? $"Shield Commander — {ip}"
                    : $"Shield Commander — {name} ({ip})";
                ConnectionStatusText = string.IsNullOrEmpty(name)
                    ? $"Connected to {ip}"
                    : $"Connected to {ip} — {name}";

                _ = _adbService.OpenSessionAsync();
                _ = SystemPage.ActivateAsync();
                _ = ActivityMonitorPage.StartAsync();

                if (CurrentPage == ProcessesPage)
                {
                    _ = ProcessesPage.StartAsync();
                }

                if (CurrentPage == AppsPage && AppsPage.Packages.Count == 0)
                {
                    AppsPage.RefreshCommand.Execute(null);
                }
            }
            else
            {
                WindowTitle = "Shield Commander — Disconnected";
                ConnectionStatusText = "Disconnected";
                _adbService.CloseSession();
                ActivityMonitorPage.Stop();
                ProcessesPage.Stop();
            }
        };
    }

    public void CloseAdbSession() => _adbService.CloseSession();

    public void NavigateTo(string tag)
    {
        var previousPage = CurrentPage;

        CurrentPage = tag switch
        {
            "Apps" => AppsPage,
            "SystemInfo" => SystemPage,
            "CPU" or "Memory" or "Disk" or "Network" or "Thermals" => SetActivityMetric(tag),
            "Processes" => ProcessesPage,
            _ => SystemPage,
        };

        if (CurrentPage == AppsPage && AppsPage.Packages.Count == 0 && IsDeviceConnected)
        {
            AppsPage.RefreshCommand.Execute(null);
        }

        // Start/stop processes polling based on page visibility
        if (CurrentPage == ProcessesPage && !ProcessesPage.IsMonitoring && IsDeviceConnected)
        {
            _ = ProcessesPage.StartAsync();
        }
        else if (previousPage == ProcessesPage && CurrentPage != ProcessesPage)
        {
            ProcessesPage.Stop();
        }
    }

    private ActivityMonitorViewModel SetActivityMetric(string metric)
    {
        ActivityMonitorPage.SelectedMetric = metric;
        return ActivityMonitorPage;
    }
}
