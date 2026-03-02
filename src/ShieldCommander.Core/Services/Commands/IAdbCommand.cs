using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal interface IAdbCommand
{
    string Name { get; }

    Task<AdbResult> ExecuteAsync(AdbRunner runner, string? deviceSerial);
}
