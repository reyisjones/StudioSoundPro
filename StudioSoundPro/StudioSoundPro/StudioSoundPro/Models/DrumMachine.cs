using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudioSoundPro.Models
{
    public class DrumMachine : SoundComponent
    {
        private readonly List<SoundComponent> _components = new List<SoundComponent>();

        public void AddPad(SoundComponent component)
        {
            _components.Add(component);
        }

        public void RemovePad(SoundComponent component)
        {
            _components.Remove(component);
        }

        public override async Task PlayAsync()
        {
            var tasks = _components.Select(c => c.PlayAsync());
            await Task.WhenAll(tasks);
        }
    }

}
