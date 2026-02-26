using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NvidiaShieldManager.Core.Models;
using NvidiaShieldManager.Core.Services;

namespace NvidiaShieldManager.UI.ViewModels;

public partial class DeviceViewModel : ViewModelBase
{
    private readonly AdbService _adbService;
    private readonly SettingsService _settingsService;
    private readonly DeviceDiscoveryService _discoveryService = new();

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private string _statusText = "Not connected";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isScanning;

    public ObservableCollection<ShieldDevice> ConnectedDevices { get; } = [];
    public ObservableCollection<SavedDevice> SavedDevices { get; } = [];
    public ObservableCollection<DiscoveredDevice> DiscoveredDevices { get; } = [];

    public DeviceViewModel(AdbService adbService, SettingsService settingsService)
    {
        _adbService = adbService;
        _settingsService = settingsService;
        LoadSavedDevices();
    }

    private void LoadSavedDevices()
    {
        SavedDevices.Clear();
        foreach (var device in _settingsService.SavedDevices.OrderByDescending(d => d.LastConnected))
            SavedDevices.Add(device);
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
            _settingsService.AddOrUpdateDevice(IpAddress, device?.DeviceName);
            LoadSavedDevices();
        }
        else
        {
            StatusText = $"Failed: {(string.IsNullOrEmpty(result.Error) ? result.Output : result.Error)}";
            IsConnected = false;
        }

        IsBusy = false;
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
        _settingsService.RemoveDevice(device.IpAddress);
        LoadSavedDevices();
    }

    [RelayCommand]
    private async Task ScanNetworkAsync()
    {
        IsScanning = true;
        StatusText = "Scanning network for devices...";
        DiscoveredDevices.Clear();

        try
        {
            var devices = await _discoveryService.ScanAsync();
            foreach (var device in devices)
                DiscoveredDevices.Add(device);

            StatusText = devices.Count > 0
                ? $"Found {devices.Count} device(s)"
                : "No devices found on network";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan failed: {ex.Message}";
        }

        IsScanning = false;
    }

    [RelayCommand]
    private async Task ConnectToDiscoveredAsync(DiscoveredDevice device)
    {
        IpAddress = device.IpAddress;
        await ConnectAsync();
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        IsBusy = true;
        await _adbService.DisconnectAllAsync();
        IsConnected = false;
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
            ConnectedDevices.Add(device);
    }
}
