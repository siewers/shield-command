using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Services;

internal sealed class AdbDeviceOperations(AdbRunner runner)
{
    public Task<AdbResult> ConnectAsync(string ipAddress, int port = 5555)
        => new ConnectDeviceCommand(ipAddress, port).ExecuteAsync(runner);

    public Task<AdbResult> DisconnectAsync(string ipAddress)
        => new DisconnectDeviceCommand(ipAddress).ExecuteAsync(runner);

    public Task<AdbResult> DisconnectAllAsync()
        => new DisconnectDeviceCommand().ExecuteAsync(runner);

    public Task<List<ShieldDevice>> GetConnectedDevicesAsync()
        => new ConnectedDevicesQuery().ExecuteAsync(runner);
}
