using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class PackageInfoQuery(string packageName) : IAdbQuery<InstalledPackage>
{
    public async Task<InstalledPackage> ExecuteAsync(AdbRunner runner)
    {
        var result = await runner.RunAdbAsync($"shell dumpsys package {packageName}");

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

        return new InstalledPackage(
            packageName, versionName, versionCode,
            installerPackageName, firstInstallTime, lastUpdateTime,
            targetSdk, minSdk, dataDir, uid, codePath);
    }
}
