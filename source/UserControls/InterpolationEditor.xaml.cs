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
    /// Logique d'interaction pour InterpolationEditor.xaml
    /// </summary>
    public partial class InterpolationEditor : UserControl
    {
        public InterpolationEditor()
        {
            InitializeComponent();
        }

        public static class Static
        {
            public static Interpolation[] DefaultInterpolations = new Interpolation[]
            {
                new Interpolation()
                {
                    Name="Linear",
                    Description="Linear interpolation between two points.",
                    InterpolationFunction = (x0, x1, y0, y1, x) => y0 + (y1 - y0) * (x - x0) / (x1 - x0),
                },

                new Interpolation()
                {
                    Name="Constant",
                    Description="",
                    InterpolationFunction = (x0, x1, y0, y1, x) =>
                    {
                        return x < x1 ? y0 : y1;
                    }
                },

                new Interpolation()
                {
                    Name="Ease-In 2",
                    Description="",
                    InterpolationFunction = (x0, x1, y0, y1, x) =>
                    {
                        double t = (x - x0) / (x1 - x0);
                        double y = y0 + (y1 - y0) * Math.Pow(t, 2);
                        return y;
                    }
                },

                new Interpolation()
                {
                    Name="Ease-In 10",
                    Description="",
                    InterpolationFunction = (x0, x1, y0, y1, x) =>
                    {
                        double t = (x - x0) / (x1 - x0);
                        double y = y0 + (y1 - y0) * Math.Pow(t, 10);
                        return y;
                    }
                },

                new Interpolation()
                {
                    Name="Ease-Out 2",
                    Description="",
                    InterpolationFunction = (x0, x1, y0, y1, x) =>
                    {
                        double t = (x - x0) / (x1 - x0);
                        double y = y0 + (y1 - y0) * (1 - Math.Pow((1 - t), 2));
                        return y;
                    }
                },

                new Interpolation()
                {
                    Name="Ease-Out 10",
                    Description="",
                    InterpolationFunction = (x0, x1, y0, y1, x) =>
                    {
                        double t = (x - x0) / (x1 - x0);
                        double y = y0 + (y1 - y0) * (1 - Math.Pow((1 - t), 10));
                        return y;
                    }
                },

                new Interpolation()
                {
                    Name="Ease-In-Out",
                    Description="",
                    InterpolationFunction = (x0, x1, y0, y1, x) =>
                    {
                        double t = (x - x0) / (x1 - x0);
                        if (t < 0.5)
                        {
                            return y0 + (y1 - y0) * (2 * Math.Pow(t, 2));
                        }
                        else
                        {
                            t = 1 - t;
                            return y1 - (y1 - y0) * (2 * Math.Pow(t, 2));
                        }
                    }
                },



            };
            
            public class Interpolation
            {
                public string Name { get; set; }
                public string Description { get; set; }

                /// <summary>
                /// The interpolation's function. Inputs : x0, x1, y0, y1, x. Outputs : y.
                /// </summary>
                public Func<double, double, double, double, double, double> InterpolationFunction { get; set; }
            }
        }
    }
}
