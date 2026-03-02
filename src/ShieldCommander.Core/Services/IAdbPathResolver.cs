namespace ShieldCommander.Core.Services;

public interface IAdbPathResolver
{
    string FindAdb();

    bool IsAvailable(string adbPath);
}
