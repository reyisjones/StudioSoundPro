using CommunityToolkit.Mvvm.ComponentModel;

namespace StudioSoundPro.UI.ViewModels;

public abstract class ViewModelBase : ObservableObject, System.IDisposable
{
    protected virtual void Dispose(bool disposing)
    {
        // Override in derived classes to dispose resources
    }

    public void Dispose()
    {
        Dispose(true);
        System.GC.SuppressFinalize(this);
    }
}
