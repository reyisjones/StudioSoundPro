using Avalonia.Controls;
using StudioSoundPro.UI.ViewModels;

namespace StudioSoundPro.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Inject storage provider into SessionViewModel when window is loaded
        Loaded += (s, e) =>
        {
            if (DataContext is MainWindowViewModel mainVm)
            {
                mainVm.Session.SetStorageProvider(StorageProvider);
            }
        };
    }
}