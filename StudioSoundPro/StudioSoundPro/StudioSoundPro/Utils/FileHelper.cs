using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace StudioSoundPro.Utils
{
    public static class FileHelper
    {
        public static async Task<StorageFile> GetFileAsync(string filePath)
        {
            return await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/" + filePath));
        }

        public static string GetFilePath(string fileName)
        {
            // Get the path to the directory where the assembly is running
            string binDirectory = AppContext.BaseDirectory;

            // Combine the path with the file name
            return Path.Combine(binDirectory, fileName);
        }
    }
}
