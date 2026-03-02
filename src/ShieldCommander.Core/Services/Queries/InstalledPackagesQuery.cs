using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class InstalledPackagesQuery : IAdbQuery<List<InstalledPackage>>
{
    private const string PackageDelimiter = "===PKG===";
    private const string SizeDelimiter = "---SIZE---";

    public async Task<List<InstalledPackage>> ExecuteAsync(IAdbRunner runner)
    {
        var cmd = "for p in $(pm list packages -3 | sed 's/package://g'); do " + $"echo '{PackageDelimiter}'\"$p\"; " + "dumpsys package $p; " + $"echo '{SizeDelimiter}'; " + "stat -c %s $(pm path $p | sed 's/package://g') 2>/dev/null; " + "done";

        var output = await runner.RunShellAsync(cmd);

        return string.IsNullOrWhiteSpace(output) ? [] : Parse(output);
    }

    public List<InstalledPackage> Parse(string output)
    {
        var packages = new List<InstalledPackage>();
        var blocks = output.Split(PackageDelimiter, StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var nameEnd = block.IndexOf('\n');
            if (nameEnd < 0)
            {
                continue;
            }

            var packageName = block[..nameEnd].Trim();
            if (packageName.Length == 0)
            {
                continue;
            }

            var sizeSplit = block.Split(SizeDelimiter, count: 2);
            var package = PackageParsing.ParseDumpsys(packageName, sizeSplit[0]);
            var codeSize = sizeSplit.Length > 1 ? PackageParsing.ParseSize(sizeSplit[1]) : null;

            if (codeSize is not null)
            {
                package = package with { CodeSize = codeSize };
            }

            packages.Add(package);
        }

        return packages.OrderBy(p => p.PackageName).ToList();
    }
}
