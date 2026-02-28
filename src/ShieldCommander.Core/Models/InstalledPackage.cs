namespace ShieldCommander.Core.Models;

public sealed record InstalledPackage(
    string PackageName,
    string? VersionName = null,
    string? VersionCode = null,
    string? InstallerPackageName = null,
    string? FirstInstallTime = null,
    string? LastUpdateTime = null,
    string? TargetSdk = null,
    string? MinSdk = null,
    string? DataDir = null,
    string? Uid = null,
    string? CodePath = null,
    string? CodeSize = null);
