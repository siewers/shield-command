using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

internal sealed class ShellBatchRunner(AdbRunner runner)
{
    public async Task<DynamicSections> ExecuteAsync(AdbCommandCollection commands)
    {
        var combinedCommand = commands.ToCombinedCommand();
        var output = await runner.RunShellWithFallbackAsync(combinedCommand);
        var result = new DynamicSections();
        commands.ApplyAll(output, result);
        return result;
    }
}
