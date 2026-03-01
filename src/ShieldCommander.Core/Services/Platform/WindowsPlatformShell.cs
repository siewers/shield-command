using System.Diagnostics;

namespace ShieldCommander.Core.Services.Platform;

public sealed class WindowsPlatformShell : IPlatformShell
{
    public void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
