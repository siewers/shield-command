using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NvidiaShieldManager.Core.Services;

namespace NvidiaShieldManager.UI.ViewModels;

public partial class SystemViewModel : ViewModelBase
{
    private readonly AdbService _adbService;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = string.Empty;

    // Static
    [ObservableProperty] private string? _model;
    [ObservableProperty] private string? _manufacturer;
    [ObservableProperty] private string? _architecture;
    [ObservableProperty] private string? _androidVersion;
    [ObservableProperty] private string? _apiLevel;
    [ObservableProperty] private string? _buildId;
    [ObservableProperty] private string? _totalRam;
    [ObservableProperty] private string? _storageTotal;

    public SystemViewModel(AdbService adbService)
    {
        _adbService = adbService;
    }

    public async Task ActivateAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusText = "Loading...";

        try
        {
            var info = await _adbService.GetDeviceInfoAsync();

            Model = info.Model;
            Manufacturer = info.Manufacturer;
            Architecture = info.Architecture;
            AndroidVersion = info.AndroidVersion;
            ApiLevel = info.ApiLevel;
            BuildId = info.BuildId;
            TotalRam = info.TotalRam;
            StorageTotal = info.StorageTotal;

            StatusText = string.Empty;
        }
        catch (Exception ex)
        {
            StatusText = $"Failed: {ex.Message}";
        }

        IsBusy = false;
    }
}
