using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

internal sealed class AdbDeviceInfoOperations(AdbRunner runner)
{
    private static readonly AdbCommandCollection DynamicCommands = DynamicSections.CreateCommands();
    private readonly ShellBatchRunner _batch = new(runner);

    public async Task<DeviceInfo> GetDeviceInfoAsync(string? deviceSerial = null)
    {
        var info = new DeviceInfo();
        await FetchStaticPropsAsync(info, AdbRunner.ShellPrefix(deviceSerial));

        var sections = await _batch.ExecuteAsync(DynamicCommands, deviceSerial);
        info.RamTotal = sections.Memory.Total;
        info.StorageTotal = sections.DiskFree?.Total;

        return info;
    }

    public async Task<SystemSnapshot> GetSystemSnapshotAsync(string? deviceSerial = null)
    {
        var s = await _batch.ExecuteAsync(DynamicCommands, deviceSerial);
        return new SystemSnapshot(s.Cpu, s.Memory.Snapshot, s.Disk, s.Network, s.Thermal);
    }

    private async Task FetchStaticPropsAsync(DeviceInfo info, string prefix)
    {
        var propTasks = new[]
        {
            runner.RunAdbAsync($"{prefix} getprop ro.product.model"),
            runner.RunAdbAsync($"{prefix} getprop ro.product.manufacturer"),
            runner.RunAdbAsync($"{prefix} getprop ro.product.cpu.abi"),
            runner.RunAdbAsync($"{prefix} getprop ro.build.version.release"),
            runner.RunAdbAsync($"{prefix} getprop ro.build.version.sdk"),
            runner.RunAdbAsync($"{prefix} getprop ro.build.display.id"),
        };

        var props = await Task.WhenAll(propTasks);

        info.Model = props[0].Success ? props[0].Output.Trim() : null;
        info.Manufacturer = props[1].Success ? props[1].Output.Trim() : null;
        info.Architecture = props[2].Success ? props[2].Output.Trim() : null;
        info.AndroidVersion = props[3].Success ? props[3].Output.Trim() : null;
        info.ApiLevel = props[4].Success ? props[4].Output.Trim() : null;
        info.BuildId = props[5].Success ? props[5].Output.Trim() : null;
    }
}
