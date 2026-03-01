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

    public Task OpenSessionAsync(string? deviceSerial = null) => _runner.OpenSessionAsync(deviceSerial);

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
    public Task<List<InstalledPackage>> GetInstalledPackagesAsync(string? deviceSerial = null) =>
        _packages.GetInstalledPackagesAsync(deviceSerial);

    public Task<InstalledPackage> GetPackageInfoAsync(
        string packageName,
        string? deviceSerial = null,
        bool includeSize = false) =>
        _packages.GetPackageInfoAsync(packageName, deviceSerial, includeSize);

    public Task<AdbResult> InstallApkAsync(string apkFilePath, string? deviceSerial = null) =>
        _packages.InstallApkAsync(apkFilePath, deviceSerial);

    public Task<AdbResult> UninstallPackageAsync(string packageName, string? deviceSerial = null) =>
        _packages.UninstallPackageAsync(packageName, deviceSerial);

    // Process operations
    public Task<ProcessDetails> GetProcessDetailsAsync(int pid, string name, string? deviceSerial = null) =>
        _processes.GetProcessDetailsAsync(pid, name, deviceSerial);

    public Task<AdbResult> KillProcessAsync(int pid, string packageName, string? deviceSerial = null) =>
        _processes.KillProcessAsync(pid, packageName, deviceSerial);

    public Task<ProcessSnapshot> GetProcessSnapshotAsync(string? deviceSerial = null) =>
        _processes.GetProcessSnapshotAsync(deviceSerial);

    // Device info
    public Task<DeviceInfo> GetDeviceInfoAsync(string? deviceSerial = null) =>
        _deviceInfo.GetDeviceInfoAsync(deviceSerial);

    public Task<SystemSnapshot> GetSystemSnapshotAsync(string? deviceSerial = null) =>
        _deviceInfo.GetSystemSnapshotAsync(deviceSerial);

    private bool CheckAdbAvailable() =>
        File.Exists(ResolvedPath) || AdbPathResolver.CanRunAdb(ResolvedPath);
}
