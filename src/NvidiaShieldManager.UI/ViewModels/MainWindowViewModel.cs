using CommunityToolkit.Mvvm.ComponentModel;
using NvidiaShieldManager.Core.Services;

namespace NvidiaShieldManager.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AdbService _adbService = new();
    private readonly SettingsService _settingsService = new();

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private bool _isDeviceConnected;

    [ObservableProperty]
    private string _windowTitle = "Nvidia Shield Manager — Disconnected";

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
        DevicePage = new DeviceViewModel(_adbService, _settingsService);
        AppsPage = new AppsViewModel(_adbService);
        InstallPage = new InstallViewModel(_adbService);
        SystemPage = new SystemViewModel(_adbService);
        ActivityMonitorPage = new ActivityMonitorViewModel(_adbService);
        ProcessesPage = new ProcessesViewModel(_adbService);
        _currentPage = SystemPage;

        DevicePage.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(DeviceViewModel.IsConnected))
                return;

            IsDeviceConnected = DevicePage.IsConnected;

            if (DevicePage.IsConnected)
            {
                var name = DevicePage.ConnectedDeviceName;
                var ip = DevicePage.IpAddress;
                WindowTitle = string.IsNullOrEmpty(name)
                    ? $"Nvidia Shield Manager — {ip}"
                    : $"Nvidia Shield Manager — {name} ({ip})";
                ConnectionStatusText = string.IsNullOrEmpty(name)
                    ? $"Connected to {ip}"
                    : $"Connected to {ip} — {name}";

                _ = SystemPage.ActivateAsync();
                _ = ActivityMonitorPage.StartAsync();
                _ = ProcessesPage.StartAsync();

                if (CurrentPage == AppsPage && AppsPage.Packages.Count == 0)
                    AppsPage.RefreshCommand.Execute(null);
            }
            else
            {
                WindowTitle = "Nvidia Shield Manager — Disconnected";
                ConnectionStatusText = "Disconnected";
                ActivityMonitorPage.Stop();
                ProcessesPage.Stop();
            }
        };
    }

    public void NavigateTo(string tag)
    {
        CurrentPage = tag switch
        {
            "Apps" => AppsPage,
            "SystemInfo" => SystemPage,
            "ActivityMonitor" => ActivityMonitorPage,
            "Processes" => ProcessesPage,
            _ => SystemPage,
        };

        if (CurrentPage == AppsPage && AppsPage.Packages.Count == 0 && IsDeviceConnected)
            AppsPage.RefreshCommand.Execute(null);
    }
}
