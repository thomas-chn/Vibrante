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



namespace Vibrante
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ContentContainer.Children.Add(new UserControls.Composer());
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

        // Round a value to the nearest multiple of another value
        public static double RoundToNearestMultiple(double value, int multiple)
        {
            return Math.Round(value / multiple) * multiple;
        }
        public static double RoundToNearestMultiple(double value, double multiple)
        {
            return Math.Round(value / multiple) * multiple;
        }

    }

    
}
