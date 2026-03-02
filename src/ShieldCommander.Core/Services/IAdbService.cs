using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

public interface IAdbService
{
    event Action? SessionLost;

    Task OpenSessionAsync();

    void CloseSession();

    Task<AdbResult> ConnectAsync(string ipAddress, int port = 5555);

    Task<AdbResult> DisconnectAsync(string ipAddress);

    Task<AdbResult> DisconnectAllAsync();

    Task<List<ShieldDevice>> GetConnectedDevicesAsync();

    Task<List<InstalledPackage>> GetInstalledPackagesAsync();

    Task<InstalledPackage> GetPackageInfoAsync(string packageName);

    Task<AdbResult> InstallApkAsync(string apkFilePath);

    Task<AdbResult> UninstallPackageAsync(string packageName);

    Task<ProcessDetails> GetProcessDetailsAsync(int pid, string name);

    Task<AdbResult> TerminateProcessAsync(int pid, string packageName);

    Task<ProcessSnapshot> GetProcessSnapshotAsync();

    Task<DeviceInfo> GetDeviceInfoAsync();

    Task<SystemSnapshot> GetSystemSnapshotAsync();
}
