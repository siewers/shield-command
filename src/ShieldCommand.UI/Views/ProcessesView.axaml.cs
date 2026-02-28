using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using ShieldCommand.UI.ViewModels;

namespace ShieldCommand.UI.Views;

public partial class ProcessesView : UserControl
{
    public ProcessesView()
    {
        InitializeComponent();
        // Tunnel so we get the event before DataGrid's own handling
        ProcessGrid.AddHandler(PointerReleasedEvent, OnGridPointerReleased, RoutingStrategies.Tunnel);
    }

    private void OnGridPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right) return;
        if (DataContext is not ProcessesViewModel vm || vm.SelectedProcess is null) return;

        e.Handled = true;
        var proc = vm.SelectedProcess;
        // Pause polling — collection mutations while a flyout is open freeze the UI
        vm.IsPollingSuspended = true;

        // MenuFlyout instead of ContextMenu: ContextMenu freezes the DataGrid
        // even with in-place collection updates (Avalonia + FluentAvaloniaUI issue)
        var flyout = new MenuFlyout
        {
            // Let the dismiss click pass through so a single click selects another row
            OverlayDismissEventPassThrough = true,
            Items =
            {
                new MenuItem
                {
                    Header = $"Kill \"{proc.Name}\" (PID {proc.Pid})",
                    Command = vm.KillProcessCommand,
                }
            }
        };

        flyout.Closed += (_, _) =>
        {
            vm.IsPollingSuspended = false;
        };

        flyout.ShowAt(ProcessGrid, true);
    }
}
