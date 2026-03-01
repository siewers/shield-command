using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ShieldCommander.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using ShieldCommander.UI.Dialogs;
using ShieldCommander.UI.ViewModels;

namespace ShieldCommander.UI.Views;

public sealed partial class ProcessesView : UserControl
{
    public ProcessesView()
    {
        InitializeComponent();
        // Tunnel so we get the event before DataGrid's own handling
        ProcessGrid.AddHandler(PointerReleasedEvent, OnGridPointerReleased, RoutingStrategies.Tunnel);
        ProcessGrid.Sorting += OnGridSorting;
        ProcessGrid.DoubleTapped += OnGridDoubleTapped;

        _memoryComparer = new MemoryComparer();
        ProcessGrid.Columns[2].CustomSortComparer = _memoryComparer;
    }

    private readonly MemoryComparer _memoryComparer;

    /// Sorts by Memory, always placing 0 (unknown) at the bottom.
    /// The DataGrid reverses the comparer for descending, so we counteract that
    /// for zero-entries by flipping the sign based on tracked direction.
    private sealed class MemoryComparer : System.Collections.IComparer
    {
        public bool Descending { get; set; }

        public int Compare(object? x, object? y)
        {
            var a = (x as ProcessInfo)?.Memory ?? 0;
            var b = (y as ProcessInfo)?.Memory ?? 0;
            var aZero = a == 0;
            var bZero = b == 0;

            if (aZero && bZero)
            {
                return 0;
            }

            if (aZero != bZero)
            {
                // In ascending mode the DataGrid uses our result directly.
                // In descending mode the DataGrid negates our result, so we
                // negate first so the double-negation keeps zeros at the bottom.
                var zeroLast = aZero ? 1 : -1;
                return Descending ? zeroLast : -zeroLast;
            }

            return a.CompareTo(b);
        }
    }

    private async void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ProcessesViewModel vm || vm.SelectedProcess is not { } proc)
        {
            return;
        }

        await ShowProcessInfoAsync(vm, proc);
    }

    // Columns whose first sort click should be descending (highest value first).
    private static readonly HashSet<string> DescendingFirstHeaders = ["% CPU", "Memory"];
    private int _lastSortedColumnIndex = -1;
    private bool _isSorting;

    private void OnGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (_isSorting)
        {
            return;
        }

        var idx = e.Column.DisplayIndex;
        var header = e.Column.Header?.ToString() ?? "";
        var isFirstClick = _lastSortedColumnIndex != idx;
        _lastSortedColumnIndex = idx;

        if (header == "Memory")
        {
            // Update comparer direction — the DataGrid handles the actual sort.
            // First click → descending (handled below); subsequent clicks toggle
            // and the DataGrid applies the sort direction itself.
            _memoryComparer.Descending = isFirstClick || !_memoryComparer.Descending;
        }

        if (!isFirstClick || !DescendingFirstHeaders.Contains(header))
        {
            return;
        }

        e.Handled = true;
        _isSorting = true;
        e.Column.Sort(ListSortDirection.Descending);
        _isSorting = false;
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
        var menu = App.Services.GetRequiredService<MenuHelper>();
        var flyout = new MenuFlyout
        {
            // Let the dismiss click pass through so a single click selects another row
            OverlayDismissEventPassThrough = true,
            Items =
            {
                menu.CreateItem("Info", "\ue2ce", () => _ = ShowProcessInfoAsync(vm, proc)),
                menu.CreateGoogleSearchItem(searchName),
                new Separator(),
                menu.CreateItem(
                    "Terminate",
                    "\ue4f6",
                    () => vm.KillProcessCommand.Execute(null),
                    isEnabled: proc.IsUserApp)
            }
        };

        flyout.Closed += (_, _) =>
        {
            vm.IsPollingSuspended = false;
        };

        flyout.ShowAt(ProcessGrid, true);
    }

    private static async Task ShowProcessInfoAsync(ProcessesViewModel vm, ProcessInfo proc)
    {
        var details = await vm.AdbService.GetProcessDetailsAsync(proc.Pid, proc.PackageName);
        InstalledPackage? package = proc.IsUserApp
            ? await vm.AdbService.GetPackageInfoAsync(proc.PackageName, includeSize: true)
            : null;

        if (await PackageInfoDialog.ShowAsync(
                details,
                package,
                proc.IsUserApp ? "Terminate" : null,
                proc.IsUserApp ? $"Are you sure you want to terminate \"{proc.Name}\" (PID {proc.Pid})?" : null))
        {
            await vm.KillProcessCommand.ExecuteAsync(null);
        }
    }
}
