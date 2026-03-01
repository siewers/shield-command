using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShieldCommander.Core.Services;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class SystemViewModel : ViewModelBase
{
    private readonly AdbService _adbService;

    [ObservableProperty]
    private string? _androidVersion;

    [ObservableProperty]
    private string? _apiLevel;

    [ObservableProperty]
    private string? _architecture;

    [ObservableProperty]
    private string? _buildId;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _manufacturer;

    // Static
    [ObservableProperty]
    private string? _model;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private long? _storageTotal;

    [ObservableProperty]
    private long? _totalRam;

    public SystemViewModel(AdbService adbService)
    {
        _adbService = adbService;
    }

    public async Task ActivateAsync()
    {
        await LoadAsync();
    }

    public void Clear()
    {
        Model = Manufacturer = Architecture = AndroidVersion = ApiLevel = BuildId = null;
        TotalRam = StorageTotal = null;
        StatusText = string.Empty;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

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
            TotalRam = info.RamTotal;
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
