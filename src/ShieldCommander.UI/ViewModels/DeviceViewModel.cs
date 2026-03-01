using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class DeviceViewModel : ViewModelBase
{
    private readonly AdbService _adbService;
    private readonly DeviceDiscoveryService _discoveryService = new();
    private readonly SettingsService _settings;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsAdbAvailable))]
    private string _adbPath;

    [ObservableProperty]
    private string _connectedDeviceName = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusText = "Not connected";

    public DeviceViewModel(AdbService adbService, SettingsService settings)
    {
        _adbService = adbService;
        _settings = settings;
        _adbPath = settings.AdbPath ?? _adbService.ResolvedPath;
        LoadSavedDevices();
        RefreshSuggestions();
        _ = ScanForSuggestionsAsync();
    }

    public string AdbPathPlaceholder => _adbService.FindAdb();

    public bool IsAdbAvailable => _adbService.IsAdbAvailable;

    public ObservableCollection<ShieldDevice> ConnectedDevices { get; } = [];

    public ObservableCollection<SavedDevice> SavedDevices { get; } = [];

    public ObservableCollection<DeviceSuggestion> DeviceSuggestions { get; } = [];

    /// Raised when the UI should show the "waiting for authorization" dialog.
    /// The func receives a CancellationToken (cancelled when the user clicks Cancel)
    /// and should return only after the dialog is closed.
    public Func<CancellationToken, Task>? ShowAuthorizationDialog { get; set; }

    partial void OnAdbPathChanged(string value)
    {
        var path = string.IsNullOrWhiteSpace(value) ? null : value;
        _settings.AdbPath = path;
        _adbService.SetAdbPath(path);
    }

    private void LoadSavedDevices()
    {
        SavedDevices.Clear();
        foreach (var device in _settings.SavedDevices.OrderByDescending(d => d.LastConnected))
        {
            SavedDevices.Add(device);
        }
    }

    private bool CanConnect() => IPAddress.TryParse(IpAddress, out _);

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        IsBusy = true;
        StatusText = $"Connecting to {IpAddress}...";

        // Try initial connect — may fail if device is off, or succeed but be unauthorized
        await _adbService.ConnectAsync(IpAddress);

        // Check if already fully authorized
        var devices = await _adbService.GetConnectedDevicesAsync();
        if (devices.Any(d => d.IpAddress.StartsWith(IpAddress)))
        {
            await OnConnectedAsync(IpAddress);
            IsBusy = false;
            return;
        }

        // Not yet connected/authorized — show dialog and poll
        if (ShowAuthorizationDialog == null)
        {
            StatusText = "Failed to connect";
            IsBusy = false;
            return;
        }

        using var cts = new CancellationTokenSource();
        var dialogTask = ShowAuthorizationDialog(cts.Token);
        var pollTask = PollForConnectionAsync(IpAddress, cts.Token);

        var completed = await Task.WhenAny(dialogTask, pollTask);

        if (completed == pollTask && await pollTask)
        {
            await cts.CancelAsync();
            await OnConnectedAsync(IpAddress);
        }
        else
        {
            await cts.CancelAsync();
            StatusText = "Connection cancelled";
            IsConnected = false;
            await _adbService.DisconnectAsync(IpAddress);
        }

        IsBusy = false;
    }

    private async Task OnConnectedAsync(string ipAddress)
    {
        StatusText = $"Connected to {ipAddress}";
        IsConnected = true;
        await RefreshDevicesAsync();

        var device = ConnectedDevices.FirstOrDefault(d => d.IpAddress.StartsWith(ipAddress));
        ConnectedDeviceName = device?.DeviceName ?? "";
        _settings.AddOrUpdateDevice(ipAddress, device?.DeviceName);
        LoadSavedDevices();
        RefreshSuggestions();
    }

    private async Task<bool> PollForConnectionAsync(string ipAddress, CancellationToken ct)
    {
        try
        {
            // Poll for up to 2 minutes (device may be off and need time to boot)
            for (var i = 0; i < 60; i++)
            {
                await Task.Delay(2000, ct);

                // Only retry adb connect every 10s (5 iterations) to handle the
                // "device was off" case without spamming auth prompts on the TV
                if (i > 0 && i % 5 == 0)
                {
                    await _adbService.ConnectAsync(ipAddress);
                }

                var devices = await _adbService.GetConnectedDevicesAsync();
                if (devices.Any(d => d.IpAddress.StartsWith(ipAddress)))
                {
                    return true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled
        }

        return false;
    }

    [RelayCommand]
    private void ToggleAutoConnect(SavedDevice device)
    {
        _settings.SetAutoConnect(device.IpAddress, !device.AutoConnect);
        LoadSavedDevices();
    }

    public async Task<bool> AutoConnectAsync()
    {
        var device = _settings.SavedDevices.FirstOrDefault(d => d.AutoConnect);
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
        _settings.RemoveDevice(device.IpAddress);
        LoadSavedDevices();
    }

    private void RefreshSuggestions()
    {
        DeviceSuggestions.Clear();
        foreach (var saved in _settings.SavedDevices.OrderByDescending(d => d.LastConnected))
        {
            DeviceSuggestions.Add(new DeviceSuggestion
            {
                IpAddress = saved.IpAddress,
                DisplayName = saved.DeviceName,
                Source = "Saved",
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
                        Source = "Discovered",
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
