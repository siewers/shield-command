namespace NvidiaShieldManager.Core.Models;

public record InstalledPackage(
    string PackageName,
    string? VersionName = null,
    string? VersionCode = null);
