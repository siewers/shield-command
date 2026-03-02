using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class DisconnectDeviceCommand(string? ipAddress = null) : IAdbCommand
{
    public string Name => "DisconnectDevice";

    public async Task<AdbResult> ExecuteAsync(AdbRunner runner)
    {
        return ipAddress is not null
            ? await runner.RunAdbAsync($"disconnect {ipAddress}")
            : await runner.RunAdbAsync("disconnect");
    }
}
