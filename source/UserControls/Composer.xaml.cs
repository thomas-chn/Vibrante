using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
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
using NAudio.Wave;
using NAudio.Dsp;
using System.Media;

namespace Vibrante.UserControls
{
    /// <summary>
    /// Logique d'interaction pour Composer.xaml
    /// </summary>
    public partial class Composer : UserControl
    {
        internal FrameworkElement clickedElement = null; // Clicked element, can also be an element belonging to a child user control
        
        internal Point lastMousePosition = new Point(0, 0); // Mouse position relative to MainGrid, updated only when needed
        internal double timeZoom = 1; // Zoom value on the time axis, common to all tracks
        internal double timePosition = 0; // Position of the time axis

        internal double playheadPositionInMs = 0; // Playhead position in time
        internal double actualTimelineEndTime; // The maximum time currently displayed on the timeline

        internal MediaPlayer mediaPlayer; // Media player used to play the sound
        internal bool isAudioPlaying = false; // Is the media player playing sound?
        internal bool isAudioEnded = true; // Has the media player finished playing the sound? (False when audio is paused)

        public Composer()
        {
            Static.currentComposer = this;
            Static.CurrentTool = Static.Tools.Point;
            
            InitializeComponent();
        }


        /// <summary>
        /// Update the Static.CurrentTool value and the tool selection UI
        /// </summary>
        private void SelectTool(Static.Tools Tool)
        {
            Static.CurrentTool = Tool;
            
            if (Tool == Static.Tools.Point)
            {
                PointTool_Ellipse1.Stroke = (SolidColorBrush)Application.Current.Resources["SelectedIcon"];
                PointTool_Ellipse2.Fill = (SolidColorBrush)Application.Current.Resources["SelectedIcon"];

                SelectionTool_Rect1.Fill = (SolidColorBrush)Application.Current.Resources["NotSelectedIcon"];
                SelectionTool_Rect2.Fill = (SolidColorBrush)Application.Current.Resources["NotSelectedIcon"];
                SelectionTool_Rect3.Fill = (SolidColorBrush)Application.Current.Resources["NotSelectedIcon"];
            }
            else if (Tool == Static.Tools.Selection)
            {
                PointTool_Ellipse1.Stroke = (SolidColorBrush)Application.Current.Resources["NotSelectedIcon"];
                PointTool_Ellipse2.Fill = (SolidColorBrush)Application.Current.Resources["NotSelectedIcon"];

                SelectionTool_Rect1.Fill = (SolidColorBrush)Application.Current.Resources["SelectedIcon"];
                SelectionTool_Rect2.Fill = (SolidColorBrush)Application.Current.Resources["SelectedIcon"];
                SelectionTool_Rect3.Fill = (SolidColorBrush)Application.Current.Resources["SelectedIcon"];
            }
        }

        /// <summary>
        /// Update the timeline (playhead included) based on Static.TimePosition, and change the value of Static.actualTimelineStartTime and Static.actualTimelineEndTime
        /// </summary>
        public void UpdateTimeline()
        {
            Timeline.Children.Clear();

            double firstPixel = Static.ConvertUnitToPixels(timePosition, Static.msPerPixel, timeZoom)-5; // Index of the first pixel to draw if the timeline starts at 0, with a margin of 5 pixels.
            int graduationSize = 50; // Size of a graduation in pixels, don't take zoom into account yet

            int firstGraduation = (int)(Math.Floor(firstPixel / graduationSize) * graduationSize); // Index of the pixel of the first graduation if the timeline starts at 0. Value <= 0.
            int lastGraduation = (int)(Math.Ceiling((firstPixel + Timeline.ActualWidth) / graduationSize) * graduationSize);

            actualTimelineEndTime = Static.ConvertPixelsToUnit(firstPixel + Timeline.ActualWidth, Static.msPerPixel, timeZoom);

            // Foreach graduation, draw a vertical line and a label with the time
            for (int i = firstGraduation; i < lastGraduation; i += graduationSize)
            {
                Label timeLabel = new Label()
                {
                    Content = Static.ConvertPixelsToUnit(i, Static.msPerPixel, timeZoom).ToString(),
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextForeground"],
                    IsHitTestVisible = false,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                timeLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity)); // Needed to center the label
                timeLabel.Margin = new Thickness(i - firstPixel - timeLabel.DesiredSize.Width / 2, 0, 0, 0);
                Timeline.Children.Add(timeLabel);

                Line line = new Line()
                {
                    X1 = i - firstPixel,
                    X2 = i - firstPixel,
                    Y1 = Timeline.ActualHeight / 1.5,
                    Y2 = Timeline.ActualHeight,
                    Stroke = (SolidColorBrush)Application.Current.Resources["TextForeground"],
                    StrokeThickness = 1,
                    IsHitTestVisible = false,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                Timeline.Children.Add(line);
            }

            UpdateTimelinePlayhead(removeCurrentPlayhead:false);
        }

        /// <summary>
        /// Update the playhead position on the timeline based on Static.playheadPositionInMs
        /// </summary>
        /// <param name="removeCurrentPlayhead">Delete the playhead before creating a new one?</param>
        public void UpdateTimelinePlayhead(bool removeCurrentPlayhead = true)
        {
            double positionInPixels = Static.ConvertUnitToPixels(playheadPositionInMs - timePosition, Static.msPerPixel, timeZoom) +5; //Calculate the position on the playhead, taking into account the 5 pixel margin of the timeline

            if (removeCurrentPlayhead)
            {
                // Remove the current playhead
                for (int i = 0; i < Timeline.Children.Count; i++)
                {
                    if (((FrameworkElement)Timeline.Children[i]).Name == "Playhead")
                    {
                        Timeline.Children.RemoveAt(i);
                        break;
                    }
                }
            }

            // Draw a new playhead
            Timeline.Children.Add(new Line()
            {
                X1 = positionInPixels,
                X2 = positionInPixels,
                Y1 = 0,
                Y2 = Timeline.ActualHeight,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Top,
                Name = "Playhead"
            });
        }
        


        /// <summary>
        /// Update the canvas of each track
        /// </summary>
        public void UpdateEveryTrackCanvas()
        {
            foreach (ComposerTrack composerTrack in TracksContainer.Children)
            {
                composerTrack.UpdateCanvas();
            }
        }

        /// <summary>
        /// Create a sound based on the points of each track and export it to a output.wav file
        /// </summary>
        public void GenerateSound()
        {
            double soundDuration = 0;
            int[] currentPointIndexes = new int[TracksContainer.Children.Count]; // The index of the current point for each track.
            double[] integralValues = new double[TracksContainer.Children.Count]; // The sum of all values for each track.

            // Order points by time and find the sound duration
            foreach (ComposerTrack composerTrack in TracksContainer.Children)
            {
                if (composerTrack.PointList.Count > 0)
                {
                    composerTrack.PointList = composerTrack.PointList.OrderBy(x => x.X).ToList(); // Order points by time
                    soundDuration = Math.Max(soundDuration, (int)composerTrack.PointList.Last().X); // Update the sound duration
                }
            }

            int sampleCount = (int)Math.Ceiling((soundDuration / 1000) * Static.sampleRate);

            WaveFileWriter waveFileWriter = new WaveFileWriter("output.wav", new WaveFormat(Static.sampleRate, 1));

            // Foreach sample
            for (int i = 0; i < sampleCount; i++)
            {
                double timeInMs = ((double)i / Static.sampleRate * 1000); // Time in ms of the current sample
                float sampleValue = 0;

                // Foreach track
                for (int j = 0; j< TracksContainer.Children.Count; j++)
                {
                    ComposerTrack composerTrack = (ComposerTrack)TracksContainer.Children[j];

                    // Skip the track if it has no points
                    if (composerTrack.PointList.Count == 0)
                    {
                        continue;
                    }

                    // Get the interpolation function
                    Func<double, double, double, double, double, double> interpolationFunction = ((composerTrack.InterpolationListCB.SelectedItem as ComboBoxItem).Tag as InterpolationEditor.Static.Interpolation).InterpolationFunction;

                    // If the sound has started on this track and there is a point after
                    if (timeInMs > composerTrack.PointList[0].X && currentPointIndexes[j] < composerTrack.PointList.Count - 1)
                    {
                        // Get coordinates of the previous point and the next point
                        double x0 = composerTrack.PointList[currentPointIndexes[j]].X;
                        double x1 = composerTrack.PointList[currentPointIndexes[j] + 1].X;
                        double y0 = composerTrack.PointList[currentPointIndexes[j]].Y;
                        double y1 = composerTrack.PointList[currentPointIndexes[j] + 1].Y;

                        // Calculate the frequency using the interpolation function
                        double frequency = interpolationFunction(x0, x1, y0, y1, timeInMs);

                        double angleIncrement = 2 * Math.PI * frequency * 1 / Static.sampleRate;
                        integralValues[j] += angleIncrement; // Add the angle increment to the previous values of this track

                        sampleValue += (float)(0.2 * Math.Sin(integralValues[j])); // Add the integral value of this track to the current sample value

                        // If the next point is reached, update the current point index
                        if (composerTrack.PointList[currentPointIndexes[j] + 1].X <= timeInMs)
                        {
                            currentPointIndexes[j] += 1;
                        }
                    }

                }
                
                waveFileWriter.WriteSample(sampleValue);

            }

            waveFileWriter.Close();
        }


        #region Events
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTimeline();
        }
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTimeline();
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            clickedElement = null;
        }

        internal void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            // Move only horizontally when moving on the timeline
            if (e.LeftButton == MouseButtonState.Pressed && clickedElement == Timeline)
            {
                int deltaXInPixels = (int)(lastMousePosition.X - e.GetPosition(MainGrid).X);
                double deltaXInMs = Static.ConvertPixelsToUnit(deltaXInPixels, Static.msPerPixel, timeZoom);

                if (timePosition + deltaXInMs < 0)
                {
                    timePosition = 0;
                }
                else
                {
                    timePosition += deltaXInMs;
                }

                UpdateTimeline();

                foreach (var composerTrack in TracksContainer.Children)
                {
                    if (composerTrack is ComposerTrack)
                    {
                        ((ComposerTrack)composerTrack).UpdateCanvas();
                    }
                }

                lastMousePosition = e.GetPosition(MainGrid);
            }

            else if (e.LeftButton == MouseButtonState.Pressed && clickedElement?.Name == "VerticalScaleBar")
            {
                ComposerTrack track = clickedElement.Tag as ComposerTrack;

                int deltaYInPixels = (int)(lastMousePosition.Y - e.GetPosition(MainGrid).Y);
                double deltaYInHz = -Static.PixelToHz(deltaYInPixels, track.pitchZoom);

                if (track.pitchPosition + deltaYInHz < 0)
                {
                    track.pitchPosition = 0;
                }
                else
                {
                    track.pitchPosition = track.pitchPosition + deltaYInHz;
                }

                track.UpdateCanvas();
                track.UpdateVerticalScaleBar();

                lastMousePosition = e.GetPosition(MainGrid);
            }
            
        }
        
        private void PointTool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectTool(Static.Tools.Point);
        }

        private void SelectionTool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectTool(Static.Tools.Selection);
        }

        private void GridTool_GridGapX_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Check if the text is a number
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                
                if (e.Text != "." || ((TextBox)sender).Text.Contains("."))
                {
                    e.Handled = true;
                }
            }
        }

        private void GridTool_GridGapX_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check if the pasted text is a number
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.V)
            {
                string pastedText = Clipboard.GetText();

                double number;
                if (!double.TryParse(pastedText, out number) || number <= 0)
                {
                    e.Handled = true;
                }
            }
        }

        private void GridTool_GridGapX_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Check if the text is a number
            double number;
            if (!double.TryParse(((TextBox)sender).Text, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.GetCultureInfo("en-US"), out number) || number <= 0)
            {
                ((TextBox)sender).Text = "1";
            }
        }



        private void GenerateControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mediaPlayer?.Stop();
            isAudioEnded = true;
            mediaPlayer?.Close();
            GenerateSound();
        }

        private void SkipToStartControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mediaPlayer.Position = new TimeSpan(0);
            timePosition = 0;
            playheadPositionInMs = 0;

            UpdateTimeline();
            UpdateEveryTrackCanvas();
        }

        private void SkipToEndControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Calculate the sound duration
            double soundDuration = 0;
            foreach (ComposerTrack composerTrack in TracksContainer.Children)
            {
                if (composerTrack.PointList.Count > 0)
                {
                    double trackDuration = composerTrack.PointList.OrderBy(x => x.X).Last().X;
                    soundDuration = Math.Max(soundDuration, trackDuration);
                }
            }

            mediaPlayer.Position = TimeSpan.FromMilliseconds(soundDuration);
            timePosition = soundDuration;
            playheadPositionInMs = soundDuration;

            UpdateTimeline();
            UpdateEveryTrackCanvas();
        }

        private async void PlayControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Is the audio is playing, pause the media player.
            if (isAudioPlaying)
            {
                isAudioPlaying = false;
                mediaPlayer.Pause();

                PlayButton_PauseRect1.Opacity = 0;
                PlayButton_PauseRect2.Opacity = 0;
                PlayButton_PlayIcon.Opacity = 1;
                PlayControl_Grid.ToolTip = "Play";
            }
            else
            {
                // Is the audio has not started, generate the sound and play it
                if (isAudioEnded)
                {
                    // If there are no tracks, return
                    if (TracksContainer.Children.Count == 0)
                        return;

                    GenerateSound();

                    mediaPlayer = new MediaPlayer();
                    mediaPlayer.MediaEnded += (s, args) => { isAudioEnded = true; };
                    mediaPlayer.Open(new Uri("output.wav", UriKind.Relative));
                    mediaPlayer.Play();

                    isAudioPlaying = true;
                    isAudioEnded = false;

                    PlayButton_PauseRect1.Opacity = 1;
                    PlayButton_PauseRect2.Opacity = 1;
                    PlayButton_PlayIcon.Opacity = 0;
                    PlayControl_Grid.ToolTip = "Pause";

                    // Loop until the audio is ended
                    while (!isAudioEnded)
                    {
                        //MainWindow.SetWindowTitle(Static.mediaPlayer.Position.TotalMilliseconds);
                        await Task.Delay(5);

                        // Don't update UI if audio is paused
                        if (!isAudioPlaying)
                        {
                            continue;
                        }
                        
                        // Media time < Displayed time   OR   Media time > Displayed time   =>   Go to the media time
                        if (mediaPlayer.Position.TotalMilliseconds < timePosition
                            || mediaPlayer.Position.TotalMilliseconds > actualTimelineEndTime)
                        {
                            timePosition = mediaPlayer.Position.TotalMilliseconds;
                            UpdateTimeline();
                            UpdateEveryTrackCanvas();
                        }

                        //double timePositionInPixels = Static.MsToPixel(Static.mediaPlayer.Position.TotalMilliseconds - Static.actualTimelineStartTime);
                        playheadPositionInMs = mediaPlayer.Position.TotalMilliseconds;
                        UpdateTimelinePlayhead();
                    }

                    mediaPlayer.Close();
                    isAudioPlaying = false;
                    timePosition = 0;

                    PlayButton_PauseRect1.Opacity = 0;
                    PlayButton_PauseRect2.Opacity = 0;
                    PlayButton_PlayIcon.Opacity = 1;
                    PlayControl_Grid.ToolTip = "Play";
                }

                // If the audio is paused, resume it
                else
                {
                    isAudioPlaying = true;
                    mediaPlayer.Play();

                    PlayButton_PauseRect1.Opacity = 1;
                    PlayButton_PauseRect2.Opacity = 1;
                    PlayButton_PlayIcon.Opacity = 0;
                    PlayControl_Grid.ToolTip = "Pause";
                }

            }
            
        }

        private void NewTrackButton_MouseEnter(object sender, MouseEventArgs e)
        {
            NewTrackButton.Background = new SolidColorBrush(Color.FromRgb(114, 114, 114));
        }
        private void NewTrackButton_MouseLeave(object sender, MouseEventArgs e)
        {
            NewTrackButton.Background = new SolidColorBrush(Color.FromRgb(74, 74, 74));
        }
        private void NewTrackButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ComposerTrack track = new ComposerTrack();
            TracksContainer.Children.Add(track);
        }

        internal void TimelineLeftClick(object sender, MouseButtonEventArgs e)
        {
            clickedElement = Timeline;
            lastMousePosition = e.GetPosition(MainGrid);
            //lastTimelineClickPosition = e.GetPosition(Timeline).X;
        }

        #endregion

        public static class Static
        {
            public static Composer currentComposer;
            
            public static int sampleRate = 44100;
            
            public const double msPerPixel = 9; // Milliseconds per pixel displayed for a zoom of 1.
            public const double hzPerPixel = 5; // Hertz per pixel displayed for a zoom of 1.

            public static Tools CurrentTool = Tools.Point;

            public enum Tools
            {
                Point,
                Selection
            }

            /// <summary>
            /// Convert a given amount of pixels to a specified unit of measurement.
            /// </summary>
            /// <param name="pixels">The number of pixels to be converted.</param>
            /// <param name="unit_per_pixel">The conversion factor between pixels and the specified unit of measurement.</param>
            /// <param name="zoom">The current zoom level.</param>
            /// <returns></returns>
            public static double ConvertPixelsToUnit(double pixels, double unit_per_pixel, double zoom)
            {
                return pixels * unit_per_pixel * zoom;
            }

            /// <summary>
            /// Convert a value in a specified unit of measurement to pixels.
            /// </summary>
            /// <param name="value">The value to be converted.</param>
            /// <param name="unit_per_pixel">The conversion factor between pixels and the specified unit of measurement.</param>
            /// <param name="zoom">The current zoom level.</param>
            /// <returns></returns>
            public static double ConvertUnitToPixels(double value, double unit_per_pixel, double zoom)
            {
                return Math.Round(value / (unit_per_pixel * zoom));
            }

            public static double PixelToHz(double pixels, double zoom)
            {
                return pixels * hzPerPixel * zoom;
            }


            public static double HzToPixel(double hz, double zoom)
            {
                return Math.Round(hz / (hzPerPixel * zoom));
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }
    }
}
