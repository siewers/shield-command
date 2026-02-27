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

    public DeviceViewModel DevicePage { get; }
    public AppsViewModel AppsPage { get; }
    public InstallViewModel InstallPage { get; }
    public SystemViewModel SystemPage { get; }
    public ActivityMonitorViewModel ActivityMonitorPage { get; }

    public MainWindowViewModel()
    {
        DevicePage = new DeviceViewModel(_adbService, _settingsService);
        AppsPage = new AppsViewModel(_adbService);
        InstallPage = new InstallViewModel(_adbService);
        SystemPage = new SystemViewModel(_adbService);
        ActivityMonitorPage = new ActivityMonitorViewModel(_adbService);
        _currentPage = DevicePage;

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

                _ = SystemPage.ActivateAsync();
                _ = ActivityMonitorPage.StartAsync();
            }
            else
            {
                WindowTitle = "Nvidia Shield Manager — Disconnected";
                ActivityMonitorPage.Stop();
            }
        };
    }

    public void NavigateTo(string tag)
    {
        CurrentPage = tag switch
        {
            "Device" => DevicePage,
            "Apps" => AppsPage,
            "Install" => InstallPage,
            "SystemInfo" => SystemPage,
            "ActivityMonitor" => ActivityMonitorPage,
            _ => DevicePage,
        };
    }
}
