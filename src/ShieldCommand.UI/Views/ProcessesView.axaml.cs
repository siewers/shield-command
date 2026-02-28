using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        ProcessGrid.Sorting += OnGridSorting;
    }

    // Columns whose first sort click should be descending (highest value first).
    private static readonly HashSet<string> DescendingFirstHeaders = ["% CPU", "Memory"];
    private int _lastSortedColumnIndex = -1;

    private void OnGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        var idx = e.Column.DisplayIndex;
        var header = e.Column.Header?.ToString() ?? "";
        var isFirstClick = _lastSortedColumnIndex != idx;
        _lastSortedColumnIndex = idx;

        if (!isFirstClick || !DescendingFirstHeaders.Contains(header))
        {
            return;
        }

        // Switching to this column — prevent the DataGrid's default Ascending sort
        // and sort Descending directly (no flicker from double-sort).
        e.Handled = true;
        e.Column.Sort(ListSortDirection.Descending);
    }

    private void OnGridPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right)
        {
            return;
        }

        if (DataContext is not ProcessesViewModel vm || vm.SelectedProcess is null)
        {
            return;
        }

        e.Handled = true;
        var proc = vm.SelectedProcess;
        // Pause polling — collection mutations while a flyout is open freeze the UI
        vm.IsPollingSuspended = true;

        var searchName = proc.FullName;

        // MenuFlyout instead of ContextMenu: ContextMenu freezes the DataGrid
        // even with in-place collection updates (Avalonia + FluentAvaloniaUI issue)
        var flyout = new MenuFlyout
        {
            // Let the dismiss click pass through so a single click selects another row
            OverlayDismissEventPassThrough = true,
            Items =
            {
                CreateMenuItem($"Search Google for \"{searchName}\"",
                    () => OpenBrowser($"https://www.google.com/search?q=what+is+%22{Uri.EscapeDataString(searchName)}%22+android")),
                new Separator(),
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

    private static MenuItem CreateMenuItem(string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        return item;
    }

    private static void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            Process.Start("xdg-open", url);
        }
    }
}
