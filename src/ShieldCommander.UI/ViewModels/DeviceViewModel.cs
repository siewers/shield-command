using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class DeviceViewModel : ViewModelBase
{
    private readonly AdbService _adbService;
    private readonly DeviceDiscoveryService _discoveryService = new();

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private string _statusText = "Not connected";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectedDeviceName = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isScanning;

    public ObservableCollection<ShieldDevice> ConnectedDevices { get; } = [];
    public ObservableCollection<SavedDevice> SavedDevices { get; } = [];
    public ObservableCollection<DeviceSuggestion> DeviceSuggestions { get; } = [];

    public DeviceViewModel(AdbService adbService)
    {
        _adbService = adbService;
        LoadSavedDevices();
        RefreshSuggestions();
        _ = ScanForSuggestionsAsync();
    }

    private void LoadSavedDevices()
    {
        SavedDevices.Clear();
        foreach (var device in AppSettingsAccessor.Settings.SavedDevices.OrderByDescending(d => d.LastConnected))
        {
            SavedDevices.Add(device);
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            StatusText = "Please enter an IP address";
            return;
        }

        IsBusy = true;
        StatusText = $"Connecting to {IpAddress}...";

        var result = await _adbService.ConnectAsync(IpAddress);

        if (result.Success)
        {
            StatusText = $"Connected to {IpAddress}";
            IsConnected = true;
            await RefreshDevicesAsync();

            var device = ConnectedDevices.FirstOrDefault(d => d.IpAddress.StartsWith(IpAddress));
            ConnectedDeviceName = device?.DeviceName ?? "";
            AppSettingsAccessor.Settings.AddOrUpdateDevice(IpAddress, device?.DeviceName);
            LoadSavedDevices();
            RefreshSuggestions();
        }
        else
        {
            StatusText = $"Failed: {(string.IsNullOrEmpty(result.Error) ? result.Output : result.Error)}";
            IsConnected = false;
        }

        IsBusy = false;
    }

    [RelayCommand]
    private void ToggleAutoConnect(SavedDevice device)
    {
        AppSettingsAccessor.Settings.SetAutoConnect(device.IpAddress, !device.AutoConnect);
        LoadSavedDevices();
    }

    public async Task<bool> AutoConnectAsync()
    {
        var device = AppSettingsAccessor.Settings.SavedDevices.FirstOrDefault(d => d.AutoConnect);
        if (device == null)
        {
            return false;
        }

        IpAddress = device.IpAddress;
        await ConnectAsync();
        return IsConnected;
    }

    [RelayCommand]
    private async Task ConnectToSavedAsync(SavedDevice device)
    {
        IpAddress = device.IpAddress;
        await ConnectAsync();
    }

    [RelayCommand]
    private void RemoveSavedDevice(SavedDevice device)
    {
        AppSettingsAccessor.Settings.RemoveDevice(device.IpAddress);
        LoadSavedDevices();
    }

    private void RefreshSuggestions()
    {
        DeviceSuggestions.Clear();
        foreach (var saved in AppSettingsAccessor.Settings.SavedDevices.OrderByDescending(d => d.LastConnected))
        {
            DeviceSuggestions.Add(new DeviceSuggestion
            {
                IpAddress = saved.IpAddress,
                DisplayName = saved.DeviceName,
                Source = "Saved"
            });
        }
    }

    private async Task ScanForSuggestionsAsync()
    {
        IsScanning = true;
        try
        {
            var devices = await _discoveryService.ScanAsync();
            var existingIps = DeviceSuggestions.Select(s => s.IpAddress).ToHashSet();
            foreach (var device in devices)
            {
                if (!existingIps.Contains(device.IpAddress))
                {
                    DeviceSuggestions.Add(new DeviceSuggestion
                    {
                        IpAddress = device.IpAddress,
                        DisplayName = device.DisplayName,
                        Source = "Discovered"
                    });
                }
            }
        }
        catch
        {
            // Scan failure is non-critical for suggestions
        }
        IsScanning = false;
    }

    [RelayCommand]
    private async Task RescanAsync()
    {
        RefreshSuggestions();
        await ScanForSuggestionsAsync();
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        IsBusy = true;
        await _adbService.DisconnectAllAsync();
        IsConnected = false;
        ConnectedDeviceName = string.Empty;
        StatusText = "Disconnected";
        ConnectedDevices.Clear();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RefreshDevicesAsync()
    {
        var devices = await _adbService.GetConnectedDevicesAsync();
        ConnectedDevices.Clear();
        foreach (var device in devices)
        {
            ConnectedDevices.Add(device);
        }
    }
}
