using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Services;

public sealed class AdbService
{
    private static readonly AdbCommandCollection DynamicCommands = DynamicSections.CreateCommands();

    private readonly AdbRunner _runner;
    private readonly ShellBatchRunner _batch;

    public event Action? SessionLost;

    public AdbService(AdbRunner runner)
    {
        _runner = runner;
        _batch = new ShellBatchRunner(runner);
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
    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        var propsTask = _runner.ExecuteAsync(new DevicePropertiesQuery());
        var sectionsTask = _batch.ExecuteAsync(DynamicCommands);

        var props = await propsTask;
        var sections = await sectionsTask;

        return new DeviceInfo
        {
            Model = props.Model,
            Manufacturer = props.Manufacturer,
            Architecture = props.Architecture,
            AndroidVersion = props.AndroidVersion,
            ApiLevel = props.ApiLevel,
            BuildId = props.BuildId,
            RamTotal = sections.Memory.Total,
            StorageTotal = sections.DiskFree?.Total,
        };
    }

    public async Task<SystemSnapshot> GetSystemSnapshotAsync()
    {
        var s = await _batch.ExecuteAsync(DynamicCommands);
        return new SystemSnapshot(s.Cpu, s.Memory.Snapshot, s.Disk, s.Network, s.Thermal);
    }
}
