using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Vibrante.UserControls
{
    /// <summary>
    /// Logique d'interaction pour SoundEditor.xaml
    /// </summary>
    public partial class SoundEditor : UserControl
    {
        public SoundEditor()
        {
            InitializeComponent();
        }


        public static class Static
        {
            public static Sound[] DefaultSounds = new Sound[]
            {
            new Sound() {Name = "Pure", Description = "Pure tone without any harmonic", Harmonics = new Harmonic[] { new Harmonic() { FrequencyRatio = 1, Amplitude = 1 } } },
            };

            public static Sound[] UserSounds = new Sound[] { };



            public class Sound
            {
                /// <summary>
                /// The sound's name.
                /// </summary>
                public string Name { get; set; }

                /// <summary>
                /// The sound's description.
                /// </summary>
                public string Description { get; set; }

                /// <summary>
                /// List of sound harmonics.
                /// </summary>
                public Harmonic[] Harmonics { get; set; }
            }

            public class Harmonic
            {
                /// <summary>
                /// Ratio of harmonic frequency to the fundamental frequency.
                /// </summary>
                public float FrequencyRatio { get; set; }

                /// <summary>
                /// Ratio of harmonic amplitude to the fundamental amplitude.
                /// </summary>
                public float Amplitude { get; set; }

            }
        }


    }

    
}
