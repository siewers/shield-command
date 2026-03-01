using ShieldCommander.Core.Models;

namespace ShieldCommander.UI.ViewModels;

public interface IActivityMonitor
{
    void Update(SystemSnapshot snapshot);
    void Clear();
    void SetWindows(TimeSpan chartWindow, TimeSpan miniWindow);
}
