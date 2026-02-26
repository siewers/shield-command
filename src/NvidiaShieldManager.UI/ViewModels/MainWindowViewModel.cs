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
    private int _selectedNavIndex;

    [ObservableProperty]
    private bool _isDeviceConnected;

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
                _ = SystemPage.ActivateAsync();
                _ = ActivityMonitorPage.StartAsync();
            }
            else
            {
                ActivityMonitorPage.Stop();
            }
        };
    }

    partial void OnSelectedNavIndexChanged(int value)
    {
        CurrentPage = value switch
        {
            0 => DevicePage,
            1 => AppsPage,
            2 => InstallPage,
            3 => SystemPage,
            4 => ActivityMonitorPage,
            _ => DevicePage,
        };
    }
}
