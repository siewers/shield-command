using System.Diagnostics;

namespace ShieldCommander.Core.Services.Platform;

public sealed class MacPlatformShell : IPlatformShell
{
    public void OpenUrl(string url)
    {
        Process.Start("open", url);
    }
}
