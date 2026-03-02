using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Services;

public sealed class AdbService : IAdbService
{
    private static readonly AdbBatchQueryCollection<DynamicSections> DynamicCommands = DynamicSections.CreateCommands();

    private readonly AdbRunner _runner;

    public event Action? SessionLost;

    public AdbService(IAdbRunner runner)
    {
        _runner = (AdbRunner)runner;
        _runner.SessionLost += () => SessionLost?.Invoke();
    }

    public Task OpenSessionAsync() => _runner.OpenSessionAsync();

    public void CloseSession() => _runner.CloseSession();

    // Device operations
    public Task<AdbResult> ConnectAsync(string ipAddress, int port = 5555) =>
        _runner.ExecuteAsync(new ConnectDeviceCommand(ipAddress, port));

    public Task<AdbResult> DisconnectAsync(string ipAddress) =>
        _runner.ExecuteAsync(new DisconnectDeviceCommand(ipAddress));

    public Task<AdbResult> DisconnectAllAsync() =>
        _runner.ExecuteAsync(new DisconnectDeviceCommand());

    public Task<List<ShieldDevice>> GetConnectedDevicesAsync() =>
        _runner.ExecuteAsync(new ConnectedDevicesQuery());

    // Package operations
    public Task<List<InstalledPackage>> GetInstalledPackagesAsync() =>
        _runner.ExecuteAsync(new InstalledPackagesQuery());

    public Task<InstalledPackage> GetPackageInfoAsync(string packageName) =>
        _runner.ExecuteAsync(new PackageInfoQuery(packageName));

    public Task<AdbResult> InstallApkAsync(string apkFilePath) =>
        _runner.ExecuteAsync(new InstallPackageCommand(apkFilePath));

    public Task<AdbResult> UninstallPackageAsync(string packageName) =>
        _runner.ExecuteAsync(new UninstallPackageCommand(packageName));

    // Process operations
    public Task<ProcessDetails> GetProcessDetailsAsync(int pid, string name) =>
        _runner.ExecuteAsync(new ProcessStatusQuery(pid, name));

    public Task<AdbResult> TerminateProcessAsync(int pid, string packageName) =>
        _runner.ExecuteAsync(new TerminateProcessCommand(pid, packageName));

    public Task<ProcessSnapshot> GetProcessSnapshotAsync() =>
        _runner.ExecuteAsync(new ProcessSnapshotQuery());

    // Device info
    public Task<DeviceInfo> GetDeviceInfoAsync() =>
        _runner.ExecuteAsync(new DeviceInfoQuery());

    public async Task<SystemSnapshot> GetSystemSnapshotAsync()
    {
        var output = await _runner.RunShellAsync(DynamicCommands.ToCombinedCommand());
        var s = new DynamicSections();
        DynamicCommands.ApplyAll(output, s);
        return new SystemSnapshot(s.Cpu, s.Memory.Snapshot, s.Disk, s.Network, s.Thermal);
    }
}
