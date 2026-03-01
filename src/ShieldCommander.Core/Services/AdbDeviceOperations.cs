using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

internal sealed class AdbDeviceOperations(AdbRunner runner)
{
    public async Task<AdbResult> ConnectAsync(string ipAddress, int port = 5555)
    {
        return await runner.RunAdbAsync($"connect {ipAddress}:{port}");
    }

    public async Task<AdbResult> DisconnectAsync(string ipAddress)
    {
        return await runner.RunAdbAsync($"disconnect {ipAddress}");
    }

    public async Task<AdbResult> DisconnectAllAsync()
    {
        return await runner.RunAdbAsync("disconnect");
    }

    public async Task<List<ShieldDevice>> GetConnectedDevicesAsync()
    {
        var result = await runner.RunAdbAsync("devices -l");
        var devices = new List<ShieldDevice>();

        if (!result.Success)
        {
            return devices;
        }

        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("List of") || line.StartsWith("*"))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts[1] != "device")
            {
                continue;
            }

            var address = parts[0];
            var model = parts
                       .FirstOrDefault(p => p.StartsWith("model:"))
                      ?.Replace("model:", "");

            devices.Add(new ShieldDevice(address, model, IsConnected: true));
        }

        return devices;
    }
}
