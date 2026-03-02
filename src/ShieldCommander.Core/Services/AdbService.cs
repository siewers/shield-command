using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

public sealed class AdbService
{
    private readonly AdbDeviceInfoOperations _deviceInfo;
    private readonly AdbDeviceOperations _devices;
    private readonly AdbPackageOperations _packages;
    private readonly AdbPathResolver _pathResolver;
    private readonly AdbProcessOperations _processes;

    private readonly AdbRunner _runner;
    private bool? _isAdbAvailable;

    public AdbService(SettingsService settings, AdbPathResolver pathResolver)
    {
        _pathResolver = pathResolver;
        ResolvedPath = settings.AdbPath ?? pathResolver.FindAdb();

        _runner = new AdbRunner(() => ResolvedPath);
        _devices = new AdbDeviceOperations(_runner);
        _packages = new AdbPackageOperations(_runner);
        _processes = new AdbProcessOperations(_runner);
        _deviceInfo = new AdbDeviceInfoOperations(_runner);
    }

    public string ResolvedPath { get; private set; }

    public bool IsAdbAvailable => _isAdbAvailable ??= CheckAdbAvailable();

    public void SetAdbPath(string? path)
    {
        ResolvedPath = string.IsNullOrWhiteSpace(path) ? _pathResolver.FindAdb() : path;
        _isAdbAvailable = null;
    }

    public string FindAdb() => _pathResolver.FindAdb();

    public Task OpenSessionAsync() => _runner.OpenSessionAsync();

    public void CloseSession() => _runner.CloseSession();

    // Device operations
    public Task<AdbResult> ConnectAsync(string ipAddress, int port = 5555) =>
        _devices.ConnectAsync(ipAddress, port);

    public Task<AdbResult> DisconnectAsync(string ipAddress) =>
        _devices.DisconnectAsync(ipAddress);

    public Task<AdbResult> DisconnectAllAsync() =>
        _devices.DisconnectAllAsync();

    public Task<List<ShieldDevice>> GetConnectedDevicesAsync() =>
        _devices.GetConnectedDevicesAsync();

    // Package operations
    public Task<List<InstalledPackage>> GetInstalledPackagesAsync() =>
        _packages.GetInstalledPackagesAsync();

    public Task<InstalledPackage> GetPackageInfoAsync(string packageName, bool includeSize = false) =>
        _packages.GetPackageInfoAsync(packageName, includeSize);

    public Task<AdbResult> InstallApkAsync(string apkFilePath) =>
        _packages.InstallApkAsync(apkFilePath);

    public Task<AdbResult> UninstallPackageAsync(string packageName) =>
        _packages.UninstallPackageAsync(packageName);

    // Process operations
    public Task<ProcessDetails> GetProcessDetailsAsync(int pid, string name) =>
        _processes.GetProcessDetailsAsync(pid, name);

    public Task<AdbResult> TerminateProcessAsync(int pid, string packageName) =>
        _processes.TerminateProcessAsync(pid, packageName);

    public Task<ProcessSnapshot> GetProcessSnapshotAsync() =>
        _processes.GetProcessSnapshotAsync();

    // Device info
    public Task<DeviceInfo> GetDeviceInfoAsync() =>
        _deviceInfo.GetDeviceInfoAsync();

    public Task<SystemSnapshot> GetSystemSnapshotAsync() =>
        _deviceInfo.GetSystemSnapshotAsync();

    private bool CheckAdbAvailable() =>
        File.Exists(ResolvedPath) || AdbPathResolver.CanRunAdb(ResolvedPath);
}
