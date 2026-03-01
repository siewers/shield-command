using System.Drawing;
using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

public sealed record AppSettings
{
    public List<SavedDevice> SavedDevices { get; init; } = [];

    public int WindowWidth { get; set; }

    public int WindowHeight { get; set; }

    public int? WindowX { get; set; }

    public int? WindowY { get; set; }

    public string? AdbPath { get; set; }

    public Size? GetWindowSize()
    {
        var windowSize = new Size(WindowWidth, WindowHeight);
        if (windowSize == Size.Empty)
        {
            return null;
        }

        return windowSize;
    }

    public Point? GetWindowPosition()
    {
        var position = new Point(WindowX.GetValueOrDefault(0), WindowY.GetValueOrDefault(0));
        if (position == Point.Empty)
        {
            return null;
        }

        return position;
    }
}
