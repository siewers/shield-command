using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Services;

internal sealed class AdbPackageOperations(AdbRunner runner)
{
    public Task<List<InstalledPackage>> GetInstalledPackagesAsync()
        => new InstalledPackagesQuery().ExecuteAsync(runner);

    public Task<InstalledPackage> GetPackageInfoAsync(string packageName)
        => new PackageInfoQuery(packageName).ExecuteAsync(runner);

    public Task<AdbResult> InstallApkAsync(string apkFilePath)
        => new InstallPackageCommand(apkFilePath).ExecuteAsync(runner);

    public Task<AdbResult> UninstallPackageAsync(string packageName)
        => new UninstallPackageCommand(packageName).ExecuteAsync(runner);
}
