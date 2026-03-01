using System.Drawing;
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

    public Size? WindowSize => _settings.GetWindowSize();

    public Point? WindowPosition => _settings.GetWindowPosition();

    public void SaveWindowBounds(Point position, Size size)
    {
        _settings.WindowX = position.X;
        _settings.WindowY = position.Y;
        _settings.WindowWidth = size.Width;
        _settings.WindowHeight = size.Height;
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
            using var settingsFileStream = File.OpenRead(_filePath);
            _settings = JsonSerializer.Deserialize<AppSettings>(settingsFileStream) ?? new AppSettings();
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
}
