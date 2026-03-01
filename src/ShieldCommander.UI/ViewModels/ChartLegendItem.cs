using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ShieldCommander.UI.ViewModels;

public sealed partial class ChartLegendItem : ObservableObject
{
    [ObservableProperty]
    private double _value = double.NaN;

    public string Name { get; init; } = string.Empty;

    public Color Color { get; init; }
}
