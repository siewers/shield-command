using System.Drawing;
using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

public interface ISettingsService
{
    IReadOnlyList<SavedDevice> SavedDevices { get; }

    Size? WindowSize { get; }

    Point? WindowPosition { get; }

    string? AdbPath { get; set; }

    void SaveWindowBounds(Point position, Size size);

    void AddOrUpdateDevice(string ipAddress, string? deviceName = null);

    void SetAutoConnect(string ipAddress, bool autoConnect);

    void RemoveDevice(string ipAddress);
}
