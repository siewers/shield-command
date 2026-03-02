using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class InstallPackageCommand(string apkFilePath) : IAdbCommand
{
    public string Name => "InstallPackage";

    public async Task<AdbResult> ExecuteAsync(AdbRunner runner)
    {
        return await runner.RunAdbAsync($"install -r \"{apkFilePath}\"");
    }
}
