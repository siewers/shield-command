using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Media;
using Avalonia.Platform;
using System.Linq;
using Avalonia.Markup.Xaml;
using FluentAvalonia.Styling;
using ShieldCommand.UI.ViewModels;
using ShieldCommand.UI.Views;

namespace ShieldCommand.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Set Nvidia green as accent color
        if (Styles[0] is FluentAvaloniaTheme theme)
        {
            theme.CustomAccentColor = Color.Parse("#76B900");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            using var iconStream = AssetLoader.Open(new Uri("avares://ShieldCommand/Assets/app-icon.png"));
            window.Icon = new WindowIcon(iconStream);

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void AboutMenuItem_Click(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            var dialog = new Window
            {
                Title = "About Shield Command",
                Width = 360,
                Height = 300,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Spacing = 6,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Thickness(24),
                    Children =
                    {
                        new Image
                        {
                            Source = new Avalonia.Media.Imaging.Bitmap(
                                AssetLoader.Open(new Uri("avares://ShieldCommand/Assets/app-icon.png"))),
                            Width = 72,
                            Height = 72,
                            Margin = new Thickness(0, 0, 0, 8),
                        },
                        new TextBlock
                        {
                            Text = "Shield Command",
                            FontSize = 18,
                            FontWeight = Avalonia.Media.FontWeight.SemiBold,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "Version 1.0.0",
                            FontSize = 13,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Foreground = Avalonia.Media.Brushes.Gray,
                        },
                        new TextBlock
                        {
                            Text = "Monitoring and app management\nfor your Nvidia Shield",
                            FontSize = 12,
                            TextAlignment = Avalonia.Media.TextAlignment.Center,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Foreground = Avalonia.Media.Brushes.Gray,
                            Margin = new Thickness(0, 8, 0, 0),
                        },
                        new Separator
                        {
                            Margin = new Thickness(0, 8),
                        },
                        new TextBlock
                        {
                            Text = "\u00a9 2026 Siewers Software. All rights reserved.",
                            FontSize = 11,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Foreground = Avalonia.Media.Brushes.Gray,
                        },
                        new TextBlock
                        {
                            Text = "Built with Avalonia UI",
                            FontSize = 10,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Foreground = Avalonia.Media.Brushes.DarkGray,
                            Margin = new Thickness(0, 4, 0, 0),
                        },
                    }
                }
            };
            dialog.ShowDialog(desktop.MainWindow);
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
