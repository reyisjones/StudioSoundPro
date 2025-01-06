using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StudioSoundPro.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StudioSoundPro.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DrumMachineView : Page
    {
        private readonly DrumMachine _drumMachine;

        public DrumMachineView()
        {
            this.InitializeComponent();

            _drumMachine = new DrumMachine();

            var kick = new DrumPad("Kick", @"Assets\Kick.wav");
            var snare = new DrumPad("Snare", @"Assets\Snare.wav");
            var hiHat = new DrumPad("Hi-Hat", @"Assets\HiHat.wav");

            _drumMachine.AddPad(kick);
            _drumMachine.AddPad(snare);
            _drumMachine.AddPad(hiHat);

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            var btnKick = new Button { Content = "Kick" };
            btnKick.Click += async (s, e) => await kick.PlayAsync();

            var btnSnare = new Button { Content = "Snare" };
            btnSnare.Click += async (s, e) => await snare.PlayAsync();

            var btnHiHat = new Button { Content = "HiHat" };
            btnHiHat.Click += async (s, e) => await hiHat.PlayAsync();

            stackPanel.Children.Add(btnKick);
            stackPanel.Children.Add(btnSnare);
            stackPanel.Children.Add(btnHiHat);

            this.Content = stackPanel;
        }
    }
}
