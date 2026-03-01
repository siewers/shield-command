using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

internal sealed class AdbDeviceInfoOperations(AdbRunner runner)
{
    private const string SectionSeparator = "____SECT____";

    public async Task<DeviceInfo> GetDeviceInfoAsync(string? deviceSerial = null)
    {
        var info = new DeviceInfo();
        var prefix = AdbRunner.ShellPrefix(deviceSerial);

        await FetchStaticPropsAsync(info, prefix);

        // Fetch dynamic sections once to populate TotalRam and StorageTotal
        var shellOutput = await FetchDynamicSectionsAsync(deviceSerial);
        shellOutput = shellOutput.Replace("\r\n", "\n");
        var sections = shellOutput.Split($"\n{SectionSeparator}\n");

        string GetSection(int index) => index < sections.Length ? sections[index] : "";

        var memoryInfo = DeviceInfoParser.ParseMemoryInfo(GetSection(0));
        var diskFree = DeviceInfoParser.ParseDiskFree(GetSection(1));

        info.RamTotal = memoryInfo.Total;
        info.StorageTotal = diskFree?.Total;

        return info;
    }

    public async Task<SystemSnapshot> GetSystemSnapshotAsync(string? deviceSerial = null)
    {
        var shellOutput = await FetchDynamicSectionsAsync(deviceSerial);
        shellOutput = shellOutput.Replace("\r\n", "\n");
        var sections = shellOutput.Split($"\n{SectionSeparator}\n");

        string GetSection(int index) => index < sections.Length ? sections[index] : "";

        var memoryInfo = DeviceInfoParser.ParseMemoryInfo(GetSection(0));
        var thermal = DeviceInfoParser.ParseThermal(GetSection(3));
        var cpu = DeviceInfoParser.ParseCpuStat(GetSection(4));
        var threadCount = DeviceInfoParser.ParseLoadAverage(GetSection(5));
        var processCount = DeviceInfoParser.ParseProcessCount(GetSection(6));
        var network = DeviceInfoParser.ParseNetDev(GetSection(7));
        var (kbRead, kbWritten) = DeviceInfoParser.ParseVmstat(GetSection(8));
        var disk = DeviceInfoParser.ParseDiskStats(GetSection(9), kbRead, kbWritten);

        return new SystemSnapshot(cpu, memoryInfo.Snapshot, disk, network, thermal, processCount, threadCount);
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

    private async Task<string> FetchDynamicSectionsAsync(string? deviceSerial)
    {
        var combinedCmd = string.Join($"; echo {SectionSeparator}; ",
        [
            "cat /proc/meminfo",
            "df -h /data",
            "uptime",
            "dumpsys thermalservice",
            "cat /proc/stat",
            "cat /proc/loadavg",
            "ls /proc/",
            "cat /proc/net/dev",
            "cat /proc/vmstat",
            "dumpsys diskstats",
        ]);

        return await runner.RunShellWithFallbackAsync(combinedCmd, deviceSerial);
    }
}
