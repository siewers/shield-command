using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class DeviceInfoQuery : IAdbQuery<DeviceInfo>
{
    private static readonly AdbBatchQueryCollection<DeviceInfo> Commands =
    [
        new ModelQuery(),
        new ManufacturerQuery(),
        new ArchitectureQuery(),
        new AndroidVersionQuery(),
        new ApiLevelQuery(),
        new BuildIdQuery(),
        new RamTotalQuery(),
        new StorageTotalQuery(),
    ];

    public async Task<DeviceInfo> ExecuteAsync(AdbRunner runner)
    {
        var output = await runner.RunShellAsync(Commands.ToCombinedCommand());
        return Parse(output);
    }

    public DeviceInfo Parse(string output)
    {
        var info = new DeviceInfo();
        Commands.ApplyAll(output, info);
        return info;
    }

    private static string? TrimProp(ReadOnlySpan<char> output)
    {
        var trimmed = output.Trim();
        return trimmed.Length > 0 ? trimmed.ToString() : null;
    }

    private sealed class ModelQuery : IAdbBatchQuery<DeviceInfo>
    {
        public string Name => nameof(DeviceInfo.Model);

        public string CommandText => "getprop ro.product.model";

        public void Apply(ReadOnlySpan<char> output, DeviceInfo target) => target.Model = TrimProp(output);
    }

    private sealed class ManufacturerQuery : IAdbBatchQuery<DeviceInfo>
    {
        public string Name => nameof(DeviceInfo.Manufacturer);

        public string CommandText => "getprop ro.product.manufacturer";

        public void Apply(ReadOnlySpan<char> output, DeviceInfo target) => target.Manufacturer = TrimProp(output);
    }

    private sealed class ArchitectureQuery : IAdbBatchQuery<DeviceInfo>
    {
        public string Name => nameof(DeviceInfo.Architecture);

        public string CommandText => "getprop ro.product.cpu.abi";

        public void Apply(ReadOnlySpan<char> output, DeviceInfo target) => target.Architecture = TrimProp(output);
    }

    private sealed class AndroidVersionQuery : IAdbBatchQuery<DeviceInfo>
    {
        public string Name => nameof(DeviceInfo.AndroidVersion);

        public string CommandText => "getprop ro.build.version.release";

        public void Apply(ReadOnlySpan<char> output, DeviceInfo target) => target.AndroidVersion = TrimProp(output);
    }

    private sealed class ApiLevelQuery : IAdbBatchQuery<DeviceInfo>
    {
        public string Name => nameof(DeviceInfo.ApiLevel);

        public string CommandText => "getprop ro.build.version.sdk";

        public void Apply(ReadOnlySpan<char> output, DeviceInfo target) => target.ApiLevel = TrimProp(output);
    }

    private sealed class BuildIdQuery : IAdbBatchQuery<DeviceInfo>
    {
        public string Name => nameof(DeviceInfo.BuildId);

        public string CommandText => "getprop ro.build.display.id";

        public void Apply(ReadOnlySpan<char> output, DeviceInfo target) => target.BuildId = TrimProp(output);
    }

    private sealed class RamTotalQuery : IAdbBatchQuery<DeviceInfo>
    {
        public string Name => nameof(DeviceInfo.RamTotal);

        public string CommandText => "grep MemTotal /proc/meminfo";

        public void Apply(ReadOnlySpan<char> output, DeviceInfo target)
        {
            var span = output.Trim();
            var colonIdx = span.IndexOf(':');
            if (colonIdx < 0)
            {
                return;
            }

            span = span[(colonIdx + 1)..].Trim();
            Span<Range> ranges = stackalloc Range[3];
            var count = span.Split(ranges, ' ', StringSplitOptions.RemoveEmptyEntries);
            if (count >= 1 && long.TryParse(span[ranges[0]], out var kb))
            {
                target.RamTotal = kb * 1024;
            }
        }
    }

    private sealed class StorageTotalQuery : IAdbBatchQuery<DeviceInfo>
    {
        public string Name => nameof(DeviceInfo.StorageTotal);

        public string CommandText => "df -h /data";

        public void Apply(ReadOnlySpan<char> output, DeviceInfo target)
        {
            var lineIndex = 0;
            ReadOnlySpan<char> dataLine = default;

            foreach (var line in output.EnumerateLines())
            {
                if (line.Trim().IsEmpty)
                {
                    continue;
                }

                lineIndex++;
                if (lineIndex == 2)
                {
                    dataLine = line;
                    break;
                }
            }

            if (dataLine.IsEmpty)
            {
                return;
            }

            Span<Range> cols = stackalloc Range[6];
            var colCount = dataLine.Split(cols, ' ', StringSplitOptions.RemoveEmptyEntries);
            if (colCount < 2)
            {
                return;
            }

            var bytes = dataLine[cols[1]].ParseSizeWithUnit();
            if (bytes > 0)
            {
                target.StorageTotal = bytes;
            }
        }
    }
}
