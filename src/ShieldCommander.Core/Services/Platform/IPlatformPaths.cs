namespace ShieldCommander.Core.Services.Platform;

public interface IPlatformPaths
{
    string AdbExecutableName { get; }
    IReadOnlyList<string> AdbSearchPaths { get; }
    string? ResolveExecutablePath(string exe);
}
