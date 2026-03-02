using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

public interface IAdbRunner
{
    event Action? SessionLost;

    Task OpenSessionAsync();

    void CloseSession();

    Task<string> RunShellAsync(string command, CancellationToken ct = default);

    Task<AdbResult> RunAdbAsync(string arguments);

    Task<AdbResult> RunAdbAsync(string arguments, bool strictCheck);
}
