using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class ConnectDeviceCommand(string ipAddress, int port = 5555) : IAdbCommand
{
    public string Name => "ConnectDevice";

    public async Task<AdbResult> ExecuteAsync(AdbRunner runner, string? deviceSerial)
    {
        return await runner.RunAdbAsync($"connect {ipAddress}:{port}");
    }
}
