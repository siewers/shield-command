namespace ShieldCommander.Core.Services;

public sealed class AdbPathProvider : IAdbPathProvider
{
    public string CurrentPath { get; set; } = string.Empty;
}
