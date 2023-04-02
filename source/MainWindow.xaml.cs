using NAudio.Utils;
using NAudio.Wave;
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
using Vibrante.UserControls;
using static Vibrante.UserControls.ComposerTrack;

namespace Vibrante
{

    public partial class MainWindow : Window
    {
        // Used to receive keyboard focus
        public static TextBox keyboardfocus = new TextBox()
        {
            Height = 0,
            Width = 0
        };
        
        public static Composer composer = new Composer();
        public static InterpolationEditor interpolationEditor = new InterpolationEditor();

        public MainWindow()
        {
            InitializeComponent();
            ContentContainer.Children.Add(keyboardfocus);
            ContentContainer.Children.Add(composer);
        }

        // Set the window title
        public static void SetWindowTitle(IFormattable content)
        {
            ((MainWindow)Application.Current.MainWindow).Title = content.ToString();
        }
        public static void SetWindowTitle(string content)
        {
            ((MainWindow)Application.Current.MainWindow).Title = content;
        }



    }


}
