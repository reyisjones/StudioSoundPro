using System.Threading.Tasks;

namespace StudioSoundPro.Models
{
    public abstract class SoundComponent
    {
        public abstract Task PlayAsync();
    }
}
