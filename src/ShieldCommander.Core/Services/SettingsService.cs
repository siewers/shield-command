using System.Text.Json;
using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private AppSettings _settings = new();

    public SettingsService(string? configDirectory = null)
    {
        var dir = configDirectory
                  ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                      "ShieldCommander");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        Load();
    }

    public IReadOnlyList<SavedDevice> SavedDevices => _settings.SavedDevices.AsReadOnly();

    public (double Width, double Height)? WindowSize
    {
        get => _settings.WindowWidth > 0 && _settings.WindowHeight > 0
            ? (_settings.WindowWidth, _settings.WindowHeight)
            : null;
    }

    public (double X, double Y)? WindowPosition
    {
        get => _settings.WindowX is not null && _settings.WindowY is not null
            ? (_settings.WindowX.Value, _settings.WindowY.Value)
            : null;
    }

    public void SaveWindowBounds(double x, double y, double width, double height)
    {
        _settings.WindowX = x;
        _settings.WindowY = y;
        _settings.WindowWidth = width;
        _settings.WindowHeight = height;
        Save();
    }

    public string? AdbPath
    {
        get => _settings.AdbPath;
        set
        {
            _settings.AdbPath = string.IsNullOrWhiteSpace(value) ? null : value;
            Save();
        }
    }

    public void AddOrUpdateDevice(string ipAddress, string? deviceName = null)
    {
        var existing = _settings.SavedDevices.FirstOrDefault(
            d => d.IpAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.LastConnected = DateTime.UtcNow;
            if (deviceName != null)
            {
                existing.DeviceName = deviceName;
            }
        }
        else
        {
            _settings.SavedDevices.Add(new SavedDevice
            {
                IpAddress = ipAddress,
                DeviceName = deviceName,
                LastConnected = DateTime.UtcNow,
            });
        }

        Save();
    }

    public void SetAutoConnect(string ipAddress, bool autoConnect)
    {
        foreach (var device in _settings.SavedDevices)
        {
            device.AutoConnect = autoConnect
                && device.IpAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase);
        }

        Save();
    }

    public void RemoveDevice(string ipAddress)
    {
        _settings.SavedDevices.RemoveAll(
            d => d.IpAddress.Equals(ipAddress, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_settings, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private sealed class AppSettings
    {
        public List<SavedDevice> SavedDevices { get; set; } = [];
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public double? WindowX { get; set; }
        public double? WindowY { get; set; }
        public string? AdbPath { get; set; }
    }
}
