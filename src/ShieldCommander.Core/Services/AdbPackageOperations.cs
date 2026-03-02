using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Services;

internal sealed class AdbPackageOperations(AdbRunner runner)
{
    public async Task<List<InstalledPackage>> GetInstalledPackagesAsync()
    {
        var packageNames = await new PackageListQuery().ExecuteAsync(runner);

        var infoTasks = packageNames.Select(name => GetPackageInfoAsync(name));
        var packages = await Task.WhenAll(infoTasks);

        return packages.OrderBy(p => p.PackageName).ToList();
    }

    public async Task<InstalledPackage> GetPackageInfoAsync(string packageName, bool includeSize = false)
    {
        var package = await new PackageInfoQuery(packageName).ExecuteAsync(runner);

        if (includeSize)
        {
            var codeSize = await MeasurePackageSizeAsync(packageName);
            package = package with { CodeSize = codeSize };
        }

        return package;
    }

    public Task<AdbResult> InstallApkAsync(string apkFilePath)
        => new InstallPackageCommand(apkFilePath).ExecuteAsync(runner);

    public Task<AdbResult> UninstallPackageAsync(string packageName)
        => new UninstallPackageCommand(packageName).ExecuteAsync(runner);

    private async Task<long?> MeasurePackageSizeAsync(string packageName)
    {
        var pmResult = await runner.RunAdbAsync($"shell pm path {packageName}");
        if (!pmResult.Success)
        {
            return null;
        }

        var statArgs = string.Join(' ', pmResult.Output
                                                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(line => line.Replace("package:", "").Trim())
                                                .Where(p => p.Length > 0));

        if (statArgs.Length == 0)
        {
            return null;
        }

        var statResult = await runner.RunAdbAsync($"shell stat -c %s {statArgs}");
        if (!statResult.Success)
        {
            return null;
        }

        long totalBytes = 0;
        foreach (var line in statResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (long.TryParse(line.Trim(), out var bytes))
            {
                totalBytes += bytes;
            }
        }

        return totalBytes;
    }
}
