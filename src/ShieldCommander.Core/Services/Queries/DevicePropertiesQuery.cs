using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class DevicePropertiesQuery : IAdbQuery<DeviceProperties>
{
    public async Task<DeviceProperties> ExecuteAsync(AdbRunner runner)
    {
        var cmd = string.Join("; echo ---; ",
            "getprop ro.product.model",
            "getprop ro.product.manufacturer",
            "getprop ro.product.cpu.abi",
            "getprop ro.build.version.release",
            "getprop ro.build.version.sdk",
            "getprop ro.build.display.id");

        var output = await runner.RunShellWithFallbackAsync(cmd);

        var parts = output.Split("---");

        return new DeviceProperties(
            Model: GetPart(parts, 0),
            Manufacturer: GetPart(parts, 1),
            Architecture: GetPart(parts, 2),
            AndroidVersion: GetPart(parts, 3),
            ApiLevel: GetPart(parts, 4),
            BuildId: GetPart(parts, 5));
    }

    private static string? GetPart(string[] parts, int index)
    {
        if (index >= parts.Length)
        {
            return null;
        }

        var value = parts[index].Trim();
        return value.Length > 0 ? value : null;
    }
}
