using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Input;
using StudioSoundPro.UI.ViewModels;
using StudioSoundPro.UI.Views;

namespace StudioSoundPro.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            // Set application name BEFORE creating the window
            Name = "Studio Sound Pro";
            
            desktop.MainWindow = new MainWindow();
            
            // Create native menu for macOS - this replaces the default Avalonia menu
            var menu = new NativeMenu();
            var appMenu = new NativeMenuItem();
            var appSubMenu = new NativeMenu();
            
            appSubMenu.Add(new NativeMenuItem("About Studio Sound Pro"));
            appSubMenu.Add(new NativeMenuItemSeparator());
            appSubMenu.Add(new NativeMenuItem("Hide Studio Sound Pro") { Gesture = KeyGesture.Parse("CMD+H") });
            appSubMenu.Add(new NativeMenuItem("Hide Others") { Gesture = KeyGesture.Parse("CMD+ALT+H") });
            appSubMenu.Add(new NativeMenuItem("Show All"));
            appSubMenu.Add(new NativeMenuItemSeparator());
            appSubMenu.Add(new NativeMenuItem("Quit Studio Sound Pro") { Gesture = KeyGesture.Parse("CMD+Q") });
            
            appMenu.Menu = appSubMenu;
            menu.Add(appMenu);
            
            NativeMenu.SetMenu(desktop.MainWindow, menu);
            // The AudioTestView will create its own DataContext via design-time support
        }

        base.OnFrameworkInitializationCompleted();
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