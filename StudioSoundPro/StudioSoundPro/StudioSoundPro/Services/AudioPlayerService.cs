using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace StudioSoundPro.Services
{
    public class AudioPlayerService
    {
        public async Task PlaySoundAsync(string soundFile)
        {
            var player = new Windows.Media.Playback.MediaPlayer();
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/" + soundFile));
            player.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);
            player.Play();
        }
    }

}
