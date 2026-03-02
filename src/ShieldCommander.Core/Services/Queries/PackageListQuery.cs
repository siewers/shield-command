namespace ShieldCommander.Core.Services.Queries;

internal sealed class PackageListQuery : IAdbQuery<List<string>>
{
    public async Task<List<string>> ExecuteAsync(AdbRunner runner)
    {
        var result = await runner.RunAdbAsync("shell pm list packages -3");

        if (!result.Success)
        {
            return [];
        }

        return result.Output
                     .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                     .Select(line => line.Replace("package:", "").Trim())
                     .Where(n => n.Length > 0)
                     .ToList();
    }
}
