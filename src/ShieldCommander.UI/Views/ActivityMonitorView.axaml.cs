using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using ShieldCommander.UI.ViewModels;

namespace ShieldCommander.UI.Views;

public sealed partial class ActivityMonitorView : UserControl
{
    private readonly Dictionary<string, Control> _metricViews = new()
    {
        ["CPU"] = new CpuView(),
        ["Memory"] = new MemoryView(),
        ["Disk"] = new DiskView(),
        ["Network"] = new NetworkView(),
        ["Thermals"] = new ThermalsView(),
    };

    public ActivityMonitorView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SubscribeToViewModel();
    }

    private ActivityMonitorViewModel? _subscribedVm;

    private void SubscribeToViewModel()
    {
        if (_subscribedVm is not null)
        {
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _subscribedVm = DataContext as ActivityMonitorViewModel;

        if (_subscribedVm is not null)
        {
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateContent(_subscribedVm.SelectedMetric);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActivityMonitorViewModel.SelectedMetric) && _subscribedVm is not null)
        {
            UpdateContent(_subscribedVm.SelectedMetric);
        }
    }

    private void UpdateContent(string metric)
    {
        MetricContent.Content = _metricViews.GetValueOrDefault(metric);
    }
}
