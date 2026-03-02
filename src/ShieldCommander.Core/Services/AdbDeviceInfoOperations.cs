using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Services;

internal sealed class AdbDeviceInfoOperations(AdbRunner runner)
{
    private static readonly AdbCommandCollection DynamicCommands = DynamicSections.CreateCommands();
    private readonly ShellBatchRunner _batch = new(runner);

    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        var propsTask = new DevicePropertiesQuery().ExecuteAsync(runner);
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
