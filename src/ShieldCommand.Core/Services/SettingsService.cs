using System.Text.Json;
using ShieldCommand.Core.Models;

namespace ShieldCommand.Core.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private AppSettings _settings = new();

    public SettingsService(string? configDirectory = null)
    {
        var dir = configDirectory
                  ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                      "ShieldCommand");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        Load();
    }

    public IReadOnlyList<SavedDevice> SavedDevices => _settings.SavedDevices.AsReadOnly();

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

    private class AppSettings
    {
        public List<SavedDevice> SavedDevices { get; set; } = [];
    }
}
