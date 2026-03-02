using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Services;

internal sealed class AdbProcessOperations(AdbRunner runner)
{
    public Task<ProcessDetails> GetProcessDetailsAsync(int pid, string name)
        => new ProcessStatusQuery(pid, name).ExecuteAsync(runner);

    public Task<AdbResult> TerminateProcessAsync(int pid, string packageName)
        => new TerminateProcessCommand(pid, packageName).ExecuteAsync(runner);

    public Task<ProcessSnapshot> GetProcessSnapshotAsync()
        => new ProcessSnapshotQuery().ExecuteAsync(runner);
}
