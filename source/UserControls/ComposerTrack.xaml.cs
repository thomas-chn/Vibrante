using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Vibrante.UserControls
{

    public partial class ComposerTrack : UserControl
    {
        public List<Point> PointList = new List<Point>();
        
        public double pitchZoom = 1;
        public double pitchPosition = 0;

        //internal Point lastMiddleClickPosition = new Point(0, 0);
        //internal double lastVScaleBarClickPosition = 0;

        //private Point? MiddleClickPosition = null;
        
        private enum ControlTypes
        {
            PointContainer
        }

        public ComposerTrack()
        {
            InitializeComponent();
            UpdateSoundList();
            UpdateInterpolationList();


        }

        /// <summary>
        /// Update the list of signatures available in the ComboBox.
        /// </summary>
        public void UpdateSoundList()
        {
            string selection = SoundListCB.SelectedValue as string;
            SoundListCB.Items.Clear();

            int new_index = 0;
            foreach(SoundEditor.Static.Sound sound in SoundEditor.Static.DefaultSounds)
            {
                if (selection == sound.Name)
                {
                    new_index = SoundListCB.Items.Add(sound.Name);
                }
                else
                {
                    SoundListCB.Items.Add(sound.Name);
                }
            }

            foreach (SoundEditor.Static.Sound sound in SoundEditor.Static.UserSounds)
            {
                if (selection == sound.Name)
                {
                    new_index = SoundListCB.Items.Add(sound.Name);
                }
                else
                {
                    SoundListCB.Items.Add(sound.Name);
                }
            }

            SoundListCB.SelectedIndex = new_index;
        }

        public void UpdateInterpolationList()
        {
            string selection = InterpolationListCB.SelectedValue as string;
            InterpolationListCB.Items.Clear();

            int new_index = 0;
            foreach (InterpolationEditor.Static.Interpolation interpolation in InterpolationEditor.Static.DefaultInterpolations)
            {
                ComboBoxItem item = new ComboBoxItem()
                {
                    Content = interpolation.Name,
                    Tag = interpolation
                };

                if (selection == interpolation.Name)
                {
                    new_index = InterpolationListCB.Items.Add(item);
                }
                else
                {
                    InterpolationListCB.Items.Add(item);
                }
            }

            InterpolationListCB.SelectedIndex = new_index;
        }
        

        public void UpdateVerticalScaleBar()
        {
            VerticalScaleBar.Children.Clear();

            double firstPixel = Composer.Static.ConvertUnitToPixels(pitchPosition, Composer.Static.hzPerPixel, pitchZoom); // Index of the first pixel to draw if the scale bar starts at 0.
            int graduationSize = 20; // Size of a graduation in pixels, don't take zoom into account yet

            int firstGraduation = (int)(Math.Floor(firstPixel / graduationSize) * graduationSize); // Index of the pixel of the first graduation if the scale bar starts at 0. Value <= 0.
            int lastGraduation = (int)(Math.Ceiling((firstPixel + VerticalScaleBar.ActualHeight) / graduationSize) * graduationSize);

            // Foreach graduation, draw a horizontal line and a label with the frequency
            for (int i = firstGraduation; i < lastGraduation; i += graduationSize)
            {
                Label freqLabel = new Label()
                {
                    Content = Composer.Static.ConvertPixelsToUnit(i, Composer.Static.hzPerPixel, pitchZoom).ToString(),
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextForeground"],
                    IsHitTestVisible = false,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FontSize = 10
                };

                freqLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity)); // Needed to center the label
                Canvas.SetBottom(freqLabel, i - firstPixel - freqLabel.DesiredSize.Height / 2);

                VerticalScaleBar.Children.Add(freqLabel);
                
                Line line = new Line()
                {
                    X1 = VerticalScaleBar.ActualWidth / 1.2,
                    X2 = VerticalScaleBar.ActualWidth,
                    Y1 = 0,
                    Y2 = 0,
                    Stroke = (SolidColorBrush)Application.Current.Resources["TextForeground"],
                    StrokeThickness = 1,
                    IsHitTestVisible = false,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                Canvas.SetBottom(line, i - firstPixel);
                VerticalScaleBar.Children.Add(line);
            }
        }

        public void UpdateCanvas()
        {
            SoundCanvas.Children.Clear();
            UpdateCanvasPoints();
            UpdateCanvasLines();
        }

        public void UpdateCanvasPoints()
        {
            int index = 0;
            foreach (Point point in PointList)
            {
                int i = index;
                
                double x = Composer.Static.ConvertUnitToPixels(point.X - Composer.Static.currentComposer.timePosition, Composer.Static.msPerPixel, Composer.Static.currentComposer.timeZoom);
                double y = SoundCanvas.ActualHeight - Composer.Static.HzToPixel(point.Y - pitchPosition, pitchZoom);

                Grid point_container = new Grid()
                {
                    Width = Properties.Settings.Default.SoundCompositer_DotSize * 2,
                    Height = Properties.Settings.Default.SoundCompositer_DotSize * 2,
                    Background = Brushes.Transparent,
                    Margin = new Thickness(x - Properties.Settings.Default.SoundCompositer_DotSize, y - Properties.Settings.Default.SoundCompositer_DotSize, 0, 0),
                    Tag = new object[] { ControlTypes.PointContainer, i }
                };


                point_container.MouseDown += (s, ev) =>
                {
                    if (ev.LeftButton == MouseButtonState.Pressed)
                    {
                        selected_point = i;
                    }
                    else if (ev.RightButton == MouseButtonState.Pressed)
                    {
                        PointList.RemoveAt(i);
                        UpdateCanvas();
                    }
                };
                
                point_container.MouseMove += (s, ev) =>
                {
                    if (ev.RightButton == MouseButtonState.Pressed)
                    {
                        PointList.RemoveAt(i);
                        UpdateCanvas();
                    }
                };

                // Pass the MouseWheel event of the point to the MouseWheel event of the canvas if the point is selected (doesn't transmit the event on its own)
                point_container.MouseWheel += (e, ev) =>
                {
                    if (ev.LeftButton == MouseButtonState.Pressed && selected_point != -1)
                    {
                        CanvasMouseWheel(e, ev);
                    }
                };

                Ellipse ellipse = new Ellipse()
                {
                    Fill = Brushes.Red,
                    Height = Properties.Settings.Default.SoundCompositer_DotSize,
                    Width = Properties.Settings.Default.SoundCompositer_DotSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };

                point_container.Children.Add(ellipse);
                SoundCanvas.Children.Add(point_container);

                index++;
            }
        }
        
        public void UpdateCanvasLines(int resolution = 5)
        {
            Point[] SortedPointList = PointList.OrderBy(p => p.X).ToArray();
            for (int i = 0; i < SortedPointList.Length; i++)
            {
                SortedPointList[i] = new Point(Composer.Static.ConvertUnitToPixels(SortedPointList[i].X - Composer.Static.currentComposer.timePosition, Composer.Static.msPerPixel, Composer.Static.currentComposer.timeZoom), SoundCanvas.ActualHeight - Composer.Static.HzToPixel(SortedPointList[i].Y - pitchPosition, pitchZoom));
            }

            Func<double, double, double, double, double, double> line_equation = ((InterpolationListCB.SelectedItem as ComboBoxItem).Tag as InterpolationEditor.Static.Interpolation).InterpolationFunction;

            for (int i = 0; i < SortedPointList.Length - 1; i++)
            {
                int segments_count = (int)Math.Ceiling((SortedPointList[i + 1].X - SortedPointList[i].X) / resolution);
                for (int j = 0; j < segments_count; j++)
                {
                    double x1 = SortedPointList[i].X + j * resolution;
                    double x2 = j < segments_count-1 ? (SortedPointList[i].X + (j + 1) * resolution) : SortedPointList[i + 1].X;
                    double y1 = line_equation(SortedPointList[i].X, SortedPointList[i + 1].X, SortedPointList[i].Y, SortedPointList[i + 1].Y, x1);
                    double y2 = line_equation(SortedPointList[i].X, SortedPointList[i + 1].X, SortedPointList[i].Y, SortedPointList[i + 1].Y, x2);

                    Line line = new Line()
                    {
                        X1 = x1,
                        X2 = x2,
                        Y1 = y1,
                        Y2 = y2,
                        Stroke = Brushes.Red,
                        StrokeThickness = 2,
                        IsHitTestVisible = false
                    };

                    SoundCanvas.Children.Add(line);
                    
                }

            }

        }

        int selected_point = -1;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateVerticalScaleBar();
        }
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateVerticalScaleBar();
        }

        private void CanvasMouseClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                CanvasMouseLeftClick(sender, e);
            }

        }
        
        private void CanvasMouseLeftClick(object sender, MouseButtonEventArgs e)
        {
            // Add a point
            if (SoundCanvas.IsMouseDirectlyOver)
            {
                double position_in_ms = Math.Round(Composer.Static.ConvertPixelsToUnit(e.GetPosition(SoundCanvas).X, Composer.Static.msPerPixel, Composer.Static.currentComposer.timeZoom), 2) + Composer.Static.currentComposer.timePosition;
                double position_in_hz = Math.Round(Composer.Static.PixelToHz(SoundCanvas.ActualHeight - e.GetPosition(SoundCanvas).Y, pitchZoom), 2) + pitchPosition;
                PointList.Add(new Point(position_in_ms, position_in_hz));
                UpdateCanvas();

                selected_point = -1;
            }
        }
        
        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            Point MousePosition = new Point(Math.Round(e.GetPosition(SoundCanvas).X, 0), Math.Round(e.GetPosition(SoundCanvas).Y, 0));
            
            //Update PositionLabel
            double position_in_ms = Math.Round(Composer.Static.ConvertPixelsToUnit(MousePosition.X, Composer.Static.msPerPixel, Composer.Static.currentComposer.timeZoom), 2) + Composer.Static.currentComposer.timePosition;
            double position_in_hz = Math.Round(Composer.Static.PixelToHz(SoundCanvas.ActualHeight - MousePosition.Y, pitchZoom), 2) + pitchPosition;
            PositionLabel.Content = "X: " + position_in_ms + "ms Y: " + position_in_hz + "Hz";

            //Move a point
            if (e.LeftButton == MouseButtonState.Pressed && selected_point != -1)
            {
                PointList[selected_point] = new Point(position_in_ms, position_in_hz);
                UpdateCanvas();
            }
        }

        private void CanvasMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            selected_point = -1;
        }

        private void CanvasMouseEnter(object sender, MouseEventArgs e)
        {
            PositionLabel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.1)));
        }

        private void CanvasMouseLeave(object sender, MouseEventArgs e)
        {
            PositionLabel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.1)));
        }

        private void VerticalScaleBar_MouseClick(object sender, MouseButtonEventArgs e)
        {
            Composer.Static.currentComposer.clickedElement = VerticalScaleBar;
            Composer.Static.currentComposer.lastMousePosition = e.GetPosition(Composer.Static.currentComposer.MainGrid);
            //lastVScaleBarClickPosition = e.GetPosition(Composer.Static.currentComposer.MainGrid).Y;
        }

        private void InterpolationCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCanvas();
        }

        private void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl);
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift);
            
            // Ctrl + Wheel => Pitch Zoom
            if (isCtrlPressed && !isShiftPressed)
            {
                double mousePositionInPixels = SoundCanvas.ActualHeight - e.GetPosition(SoundCanvas).Y;
                double mousePositionInHertz1 = Composer.Static.PixelToHz(mousePositionInPixels, pitchZoom);

                double divider = e.Delta < 0 ? 0.5 : 2; 

                if (pitchZoom / divider >= 0.01)
                {
                    pitchZoom /= divider;
                }

                double mousePositionInHertz2 = Composer.Static.PixelToHz(mousePositionInPixels, pitchZoom);
                
                pitchPosition += (mousePositionInHertz1 - mousePositionInHertz2);
                pitchPosition = (pitchPosition < 0) ? 0 : pitchPosition;

                UpdateVerticalScaleBar();
            }

            // Ctrl + Shift + Wheel => Time Zoom
            else if (isCtrlPressed && isShiftPressed)
            {
                double mousePositionInPixels = e.GetPosition(SoundCanvas).X;
                double mousePositionInMs1 = Composer.Static.ConvertPixelsToUnit(mousePositionInPixels, Composer.Static.msPerPixel, Composer.Static.currentComposer.timeZoom);

                double divider = e.Delta < 0 ? 0.5 : 2;

                if (Composer.Static.currentComposer.timeZoom / divider >= 0.01)
                {
                    Composer.Static.currentComposer.timeZoom /= divider;
                }

                double mousePositionInMs2 = Composer.Static.ConvertPixelsToUnit(mousePositionInPixels, Composer.Static.msPerPixel, Composer.Static.currentComposer.timeZoom);

                Composer.Static.currentComposer.timePosition += (mousePositionInMs1 - mousePositionInMs2);
                Composer.Static.currentComposer.timePosition = (Composer.Static.currentComposer.timePosition < 0) ? 0 : Composer.Static.currentComposer.timePosition;

                Composer.Static.currentComposer.UpdateTimeline();
                Composer.Static.currentComposer.UpdateEveryTrackCanvas();
            }

            // Shift + Wheel => Time Panning
            else if (isShiftPressed)
            {
                int deltaXInPixels = (int)Math.Round(e.Delta * Settings.Default.HorizontalPanningMouseWheelSensi);
                double deltaXInMs = Composer.Static.ConvertPixelsToUnit(deltaXInPixels, Composer.Static.msPerPixel, Composer.Static.currentComposer.timeZoom);

                if (Composer.Static.currentComposer.timePosition + deltaXInMs < 0)
                {
                    Composer.Static.currentComposer.timePosition = 0;
                }
                else
                {
                    Composer.Static.currentComposer.timePosition += deltaXInMs;
                }

                Composer.Static.currentComposer.UpdateTimeline();
                Composer.Static.currentComposer.UpdateEveryTrackCanvas();
            }

            // Only Wheel => Pitch Panning
            else
            {
                int deltaYInPixels = (int)Math.Round(e.Delta * Settings.Default.VerticalPanningMouseWheelSensi);
                double deltaYInHz = Composer.Static.PixelToHz(deltaYInPixels, pitchZoom);

                if (pitchPosition + deltaYInHz < 0)
                {
                    pitchPosition = 0;
                }
                else
                {
                    pitchPosition = pitchPosition + deltaYInHz;
                }

                UpdateVerticalScaleBar();
            }

            CanvasMouseMove(sender, e); // Simulate mouse movement to refresh UI
            UpdateCanvas();
        }
    }
}
