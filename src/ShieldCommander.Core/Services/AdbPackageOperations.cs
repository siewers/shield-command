using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

internal sealed class AdbPackageOperations(AdbRunner runner)
{
    public async Task<List<InstalledPackage>> GetInstalledPackagesAsync(string? deviceSerial = null)
    {
        var deviceArg = AdbRunner.DeviceArg(deviceSerial);
        var result = await runner.RunAdbAsync($"{deviceArg} shell pm list packages -3".Trim());

        if (!result.Success)
        {
            return [];
        }

        var packageNames = result.Output
                                 .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(StripPrefix)
                                 .Where(n => n.Length > 0)
                                 .ToList();

        var infoTasks = packageNames.Select(name => GetPackageInfoAsync(name, deviceSerial));
        var packages = await Task.WhenAll(infoTasks);

        return packages.OrderBy(p => p.PackageName).ToList();
    }

    public async Task<InstalledPackage> GetPackageInfoAsync(
        string packageName,
        string? deviceSerial = null,
        bool includeSize = false)
    {
        var deviceArg = AdbRunner.DeviceArg(deviceSerial);
        var result = await runner.RunAdbAsync($"{deviceArg} shell dumpsys package {packageName}".Trim());

        if (!result.Success)
        {
            return new InstalledPackage(packageName);
        }

        string? versionName = null, versionCode = null, installerPackageName = null;
        string? firstInstallTime = null, lastUpdateTime = null;
        string? targetSdk = null, minSdk = null, dataDir = null, uid = null, codePath = null;

        foreach (var line in result.Output.Split('\n'))
        {
            var trimmed = line.Trim();

            if (versionName == null && trimmed.StartsWith("versionName="))
            {
                versionName = trimmed["versionName=".Length..];
            }
            else if (versionCode == null && trimmed.StartsWith("versionCode="))
            {
                foreach (var part in trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (versionCode == null && part.StartsWith("versionCode="))
                    {
                        versionCode = part["versionCode=".Length..];
                    }
                    else if (minSdk == null && part.StartsWith("minSdk="))
                    {
                        minSdk = part["minSdk=".Length..];
                    }
                    else if (targetSdk == null && part.StartsWith("targetSdk="))
                    {
                        targetSdk = part["targetSdk=".Length..];
                    }
                }
            }
            else if (installerPackageName == null && trimmed.StartsWith("installerPackageName="))
            {
                installerPackageName = trimmed["installerPackageName=".Length..];
            }
            else if (firstInstallTime == null && trimmed.StartsWith("firstInstallTime="))
            {
                firstInstallTime = trimmed["firstInstallTime=".Length..];
            }
            else if (lastUpdateTime == null && trimmed.StartsWith("lastUpdateTime="))
            {
                lastUpdateTime = trimmed["lastUpdateTime=".Length..];
            }
            else if (dataDir == null && trimmed.StartsWith("dataDir="))
            {
                dataDir = trimmed["dataDir=".Length..];
            }
            else if (codePath == null && trimmed.StartsWith("codePath="))
            {
                codePath = trimmed["codePath=".Length..];
            }
            else if (uid == null && trimmed.StartsWith("userId="))
            {
                uid = trimmed["userId=".Length..];
            }
        }

        long? codeSize = null;
        if (includeSize)
        {
            codeSize = await MeasurePackageSizeAsync(packageName, deviceArg);
        }

        return new InstalledPackage(
            packageName, versionName, versionCode,
            installerPackageName, firstInstallTime, lastUpdateTime,
            targetSdk, minSdk, dataDir, uid, codePath, codeSize);
    }

    public async Task<AdbResult> InstallApkAsync(string apkFilePath, string? deviceSerial = null)
    {
        var deviceArg = AdbRunner.DeviceArg(deviceSerial);
        return await runner.RunAdbAsync($"{deviceArg} install -r \"{apkFilePath}\"".Trim());
    }

    public async Task<AdbResult> UninstallPackageAsync(string packageName, string? deviceSerial = null)
    {
        var deviceArg = AdbRunner.DeviceArg(deviceSerial);
        return await runner.RunAdbAsync($"{deviceArg} uninstall {packageName}".Trim());
    }

    private async Task<long?> MeasurePackageSizeAsync(string packageName, string deviceArg)
    {
        var pmResult = await runner.RunAdbAsync($"{deviceArg} shell pm path {packageName}".Trim());
        if (!pmResult.Success)
        {
            return null;
        }

        var statArgs = string.Join(' ', pmResult.Output
                                                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(StripPrefix)
                                                .Where(p => p.Length > 0));

        if (statArgs.Length == 0)
        {
            return null;
        }

        var statResult = await runner.RunAdbAsync($"{deviceArg} shell stat -c %s {statArgs}".Trim());
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

    private static string StripPrefix(string line) =>
        line.Replace("package:", "").Trim();
}
