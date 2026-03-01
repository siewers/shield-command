using System.Diagnostics;

namespace ShieldCommander.Core.Services.Platform;

public sealed class LinuxPlatformShell : IPlatformShell
{
    public void OpenUrl(string url)
    {
        Process.Start("xdg-open", url);
    }
}
