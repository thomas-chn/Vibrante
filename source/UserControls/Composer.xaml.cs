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
using Vibrante.Properties;
using System.Diagnostics;

namespace Vibrante.UserControls
{

    public partial class Composer : UserControl
    {
        internal int timelineLeftMargin = 5; // Margin on the left of the timeline, in pixels
        internal int timelinePlayheadWidth = 1; // Width of the playhead, in pixels
        
        internal FrameworkElement clickedElement = null; // Clicked element, can also be an element belonging to a child user control

        internal double timelineClickPosition = 0; // Position of the mouse on the timeline (x-axis) when the user clicked on it
        
        internal double playheadPositionInMs = 0; // Playhead position in time
        internal double actualTimelineEndTime; // The maximum time currently displayed on the timeline

        internal bool snapToGridEnabled = false; // Is the snap-to-grid feature enabled?
        internal bool stereoEnabled = false; // Is the sound in stereo?

        internal Point lastMousePosition = new Point(0, 0); // Mouse position relative to MainGrid, updated only when needed
        internal double timeZoom = 1; // Zoom value on the time axis, common to all tracks
        internal double timePosition = 0; // Position of the time axis

        internal enum audioPlayerStates { Playing, Paused, Stopped };
        internal MediaPlayer audioPlayer = new MediaPlayer(); // Media player used to play the sound
        internal audioPlayerStates audioPlayerState = audioPlayerStates.Stopped; // State of the media player


        public Composer()
        {
            audioPlayer.MediaEnded += (s, args) => { audioPlayerState = audioPlayerStates.Stopped; };

            InitializeComponent();

            Resources["SnapToGridIconColor"] = Resources["NotSelectedIcon"];
        }

        /// <summary>
        /// Update the timeline (playhead included) based on Static.TimePosition, and change the value of Static.actualTimelineStartTime and Static.actualTimelineEndTime
        /// </summary>
        public void UpdateTimeline()
        {
            Timeline.Children.Clear();

            double firstPixel = Static.ConvertUnitToPixels(timePosition, Settings.Default.TimelineMsPerPixel, timeZoom) - timelineLeftMargin; // Index of the first pixel to draw if the timeline starts at 0, taking into account the margin
            int graduationSize = 50; // Size of a graduation in pixels

            int firstGraduation = (int)(Math.Floor(firstPixel / graduationSize) * graduationSize); // Index of the pixel of the first graduation if the timeline starts at 0. Value <= 0.
            int lastGraduation = (int)(Math.Ceiling((firstPixel + Timeline.ActualWidth) / graduationSize) * graduationSize);

            actualTimelineEndTime = Static.ConvertPixelsToUnit(firstPixel + Timeline.ActualWidth, Settings.Default.TimelineMsPerPixel, timeZoom);

            // Foreach graduation, draw a vertical line and a label with the time
            for (int i = firstGraduation; i < lastGraduation; i += graduationSize)
            {
                Label timeLabel = new Label()
                {
                    Content = Static.ConvertPixelsToUnit(i, Settings.Default.TimelineMsPerPixel, timeZoom).ToString(),
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
            double positionInPixels = Static.ConvertUnitToPixels(playheadPositionInMs - timePosition, Settings.Default.TimelineMsPerPixel, timeZoom) + timelineLeftMargin; //Calculate the position on the playhead, taking into account the margin

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
                StrokeThickness = timelinePlayheadWidth,
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
                double deltaXInMs = Static.ConvertPixelsToUnit(deltaXInPixels, Settings.Default.TimelineMsPerPixel, timeZoom);

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
                double deltaYInHz = -Static.ConvertPixelsToUnit(deltaYInPixels, Settings.Default.PitchScaleHzPerPixel, track.pitchTab.verticalZoom);

                if (track.pitchTab.verticalPosition + deltaYInHz < 0)
                {
                    track.pitchTab.verticalPosition = 0;
                }
                else
                {
                    track.pitchTab.verticalPosition = track.pitchTab.verticalPosition + deltaYInHz;
                }

                track.UpdateCanvas();
                track.UpdateVerticalScaleBar();

                lastMousePosition = e.GetPosition(MainGrid);
            }
            
        }
        
        private void SnapToGrid_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (snapToGridEnabled)
            {
                snapToGridEnabled = false;
                Resources["SnapToGridIconColor"] = Resources["NotSelectedIcon"];
            }
            else
            {
                snapToGridEnabled = true;
                Resources["SnapToGridIconColor"] = Resources["SelectedIcon"];
            }
        }

        private void GenerateControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (audioPlayerState != audioPlayerStates.Stopped)
            {
                audioPlayer.Stop();
            }

            audioPlayer.Close();
            SoundGenerator.Generate();
        }
        private void SkipToStartControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            audioPlayer.Position = new TimeSpan(0);
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
                if (composerTrack.pitchTab.pointList.Count > 0)
                {
                    double trackDuration = composerTrack.pitchTab.pointList.OrderBy(x => x.X).Last().X;
                    soundDuration = Math.Max(soundDuration, trackDuration);
                }
            }

            audioPlayer.Position = TimeSpan.FromMilliseconds(soundDuration);
            timePosition = soundDuration;
            playheadPositionInMs = soundDuration;

            UpdateTimeline();
            UpdateEveryTrackCanvas();
        }
        private async void PlayControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // If the audio is stopped (not started or ended), open the sound file
            if (audioPlayerState == audioPlayerStates.Stopped)
            {
                // Check if the output sound file exists
                if (!System.IO.File.Exists("output.wav"))
                    return;

                TimeSpan audioPlayerPosition = audioPlayer.Position; // Save the media player position before it is reset by Open()
                audioPlayer.Open(new Uri("output.wav", UriKind.Relative));
                audioPlayer.Position = audioPlayerPosition;
            }

            // If the audio is paused or stopped, play the sound
            if (audioPlayerState != audioPlayerStates.Playing)
            {
                audioPlayerState = audioPlayerStates.Playing;
                audioPlayer.Play();

                // Update GUI
                PlayButton_PauseRect1.Opacity = 1;
                PlayButton_PauseRect2.Opacity = 1;
                PlayButton_PlayIcon.Opacity = 0;
                PlayControl_Grid.ToolTip = "Pause";

                // Loop while audio is playing
                while (audioPlayerState == audioPlayerStates.Playing)
                {
                    await Task.Delay(5);

                    // If the current time is outside the displayed timeline, change the timeline position
                    if (audioPlayer.Position.TotalMilliseconds < timePosition || audioPlayer.Position.TotalMilliseconds > actualTimelineEndTime)
                    {
                        timePosition = audioPlayer.Position.TotalMilliseconds;
                        UpdateTimeline();
                        UpdateEveryTrackCanvas();
                    }

                    // Update the playhead position
                    playheadPositionInMs = audioPlayer.Position.TotalMilliseconds;
                    UpdateTimelinePlayhead();
                }

                // If audio is ended, close the sound file
                if (audioPlayerState == audioPlayerStates.Stopped)
                {
                    audioPlayer.Close();
                    audioPlayer.Position = TimeSpan.Zero;
                    //timePosition = 0;
                    
                    PlayButton_PauseRect1.Opacity = 0;
                    PlayButton_PauseRect2.Opacity = 0;
                    PlayButton_PlayIcon.Opacity = 1;
                    PlayControl_Grid.ToolTip = "Play";
                }
            }

            // If the audio is playing, pause the audio player
            if (audioPlayerState == audioPlayerStates.Playing)
            {
                audioPlayerState = audioPlayerStates.Paused;
                audioPlayer.Pause();

                // Update GUI
                PlayButton_PauseRect1.Opacity = 0;
                PlayButton_PauseRect2.Opacity = 0;
                PlayButton_PlayIcon.Opacity = 1;
                PlayControl_Grid.ToolTip = "Play";
            }
        }

        private void StereoCheckBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            stereoEnabled = !stereoEnabled;
            StereoCheckMark.Visibility = stereoEnabled ? Visibility.Visible : Visibility.Hidden;
            StereoCheckBox_Border.BorderBrush = (SolidColorBrush)(stereoEnabled ? Resources["SelectedIcon"] : Resources["ButtonBorder"]);

            // For each track
            foreach (ComposerTrack composerTrack in TracksContainer.Children)
            {
                // If the track is on the panning tab, update the alert message visibility
                if (composerTrack.currentTab == composerTrack.panningTab)
                {
                    composerTrack.StereoDisabledAlert_Grid.Visibility = stereoEnabled ? Visibility.Hidden : Visibility.Visible;
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

        internal void Timeline_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            clickedElement = Timeline;
            lastMousePosition = e.GetPosition(MainGrid);
            timelineClickPosition = e.GetPosition(Timeline).X;
        }
        private void Timeline_MouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            // If the mouse has not been moved horizontally, set the playhead and the audio player position
            if (e.GetPosition(Timeline).X == timelineClickPosition)
            {
                playheadPositionInMs = Static.ConvertPixelsToUnit(timelineClickPosition - timelineLeftMargin + 1, Settings.Default.TimelineMsPerPixel, timeZoom) + timePosition;
                audioPlayer.Position = TimeSpan.FromMilliseconds(playheadPositionInMs);
                UpdateTimelinePlayhead();
            }
        }

        #endregion

        public static class Static
        {
            public static int sampleRate = 44100;
            
            //public const double msPerPixel = 10; // Milliseconds per pixel displayed for a zoom of 1.
            //public const double hzPerPixel = 5; // Hertz per pixel displayed for a zoom of 1.
            public const double dbPerPixel = 5; // Decibel per pixel displayed for a zoom of 1.
            

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

            /*
            public static double PixelToHz(double pixels, double zoom)
            {
                return pixels * hzPerPixel * zoom;
            }


            public static double HzToPixel(double hz, double zoom)
            {
                return Math.Round(hz / (hzPerPixel * zoom));
            }*/
        }
    }
}
