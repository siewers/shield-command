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

    public Point? GetWindowPosition()
    {
            
    }
}
