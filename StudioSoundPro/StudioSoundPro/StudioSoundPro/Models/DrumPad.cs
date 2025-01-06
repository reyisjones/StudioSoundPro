using StudioSoundPro.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;

namespace StudioSoundPro.Models
{
    public class DrumPad : SoundComponent
    {
        private readonly string _name;
        private readonly string _soundFile;

        public string SoundFile => _soundFile;

        public DrumPad(string name, string soundFile)
        {
            if (string.IsNullOrEmpty(soundFile))
                throw new ArgumentException("Sound file path cannot be null or empty.", nameof(soundFile));

            _name = name;
            _soundFile = soundFile;
        }

        public override async Task PlayAsync()
        {
            if (string.IsNullOrEmpty(_soundFile))
                throw new ArgumentException("Sound file path cannot be null or empty.");

            var player = new Windows.Media.Playback.MediaPlayer();
            string filePath = FileHelper.GetFilePath(_soundFile);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"File found: {filePath}");
            }
            else
            {
                Console.WriteLine("File not found!");
            }

            try
            {
                var file = await StorageFile.GetFileFromPathAsync(filePath);
                player.Source = MediaSource.CreateFromStorageFile(file);
                player.Play();
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException($"The sound file '{_soundFile}' could not be found.", ex);
            }
        }
    }
}
