using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;

namespace ShieldCommander.Core.Services;

internal sealed class AdbDeviceOperations(AdbRunner runner)
{
    public Task<AdbResult> ConnectAsync(string ipAddress, int port = 5555)
        => new ConnectDeviceCommand(ipAddress, port).ExecuteAsync(runner);

    public Task<AdbResult> DisconnectAsync(string ipAddress)
        => new DisconnectDeviceCommand(ipAddress).ExecuteAsync(runner);

    public Task<AdbResult> DisconnectAllAsync()
        => new DisconnectDeviceCommand().ExecuteAsync(runner);

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
