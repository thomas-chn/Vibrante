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
using System.Threading;

namespace Vibrante.UserControls
{

    public partial class Composer : UserControl
    {
        internal List<System.Drawing.PointF> copiedPoints = new List<System.Drawing.PointF>(); // Store the points copied from a track
        
        internal int timelineLeftMargin = 5; // Margin on the left of the timeline, in pixels

        // Mouse variables
        internal FrameworkElement clickedElement = null; // Clicked element, can also be an element belonging to a child user control
        internal float timelineClickPosition = 0; // Position of the mouse on the timeline (x-axis) when the user clicked on it
        internal Point lastMousePosition = new Point(0, 0); // Mouse position relative to MainGrid, updated only when needed
        
        // Composer settings
        internal bool snapToGridEnabled = false; // Is the snap-to-grid feature enabled?
        internal bool stereoEnabled = false; // Is the sound in stereo?

        // Horizontal scale properties
        internal float timeZoom = 1; // Zoom value on the time axis, common to all tracks
        internal float timePosition = 0; // Position of the time axis
        internal float msPerGrad = 50;
        internal int pixelsPerGrad = 100;
        internal float msToPixelCoeff = 100f / 50f; // pixelsPerGrad / msPerGrad
        internal float pixelToMsCoeff = 50f / 100f; // msPerGrad / pixelsPerGrad

        private bool _hasAudioChanged = false;
        internal bool hasAudioChanged
        {
            get
            {
                return _hasAudioChanged;
            }
            set
            {
                _hasAudioChanged = value;
                
                if (Resources.Count != 0)
                {
                    if (value)
                    {
                        PlayButton_Border.BorderBrush = (SolidColorBrush)Resources["ButtonBorder"];
                    }
                    else
                    {
                        PlayButton_Border.BorderBrush = (SolidColorBrush)Resources["GeneratedColor"];
                    }
                }
            }
        }
        private bool isAudioLoaded = false;
        
        internal float playheadPositionInMs = 0; // Playhead position in time
        internal float actualTimelineEndTime; // The maximum time currently displayed on the timeline
        internal enum audioPlayerStates { Playing, Paused, Stopped };
        internal MediaPlayer audioPlayer = new MediaPlayer(); // Media player used to play the sound
        internal audioPlayerStates audioPlayerState = audioPlayerStates.Stopped; // State of the media player
        
        private float currentAudioPlayerDuration = 0; // Duration of the current audio file, in milliseconds

        public Composer()
        {
            hasAudioChanged = false;
            
            CompositionTarget.Rendering += OnRendering;
            
            audioPlayer.MediaEnded += (s, args) => {
                audioPlayerState = audioPlayerStates.Stopped;
                audioPlayer.Close();
                audioPlayer.Position = TimeSpan.Zero;
                playheadPositionInMs = 0;

                PlayButton_PauseRect1.Opacity = 0;
                PlayButton_PauseRect2.Opacity = 0;
                PlayButton_PlayIcon.Opacity = 1;
                PlayControl_Grid.ToolTip = "Play";
            };

            InitializeComponent();

            Resources["SnapToGridIconColor"] = Resources["NotSelectedIcon"];
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (audioPlayerState == audioPlayerStates.Playing)
            {
                // Update the playhead position
                playheadPositionInMs = Math.Min((float)audioPlayer.Position.TotalMilliseconds, currentAudioPlayerDuration);
                
                // If the current time is outside the displayed timeline, change the timeline position
                if (audioPlayer.Position.TotalMilliseconds < timePosition || audioPlayer.Position.TotalMilliseconds > actualTimelineEndTime)
                {
                    timePosition = (float)audioPlayer.Position.TotalMilliseconds;
                    
                    UpdateEveryTrackCanvas();
                }

                UpdateTimeline();
            }
        }


        /// <summary>
        /// Update the timeline (playhead included) based on Static.TimePosition, and change the value of Static.actualTimelineStartTime and Static.actualTimelineEndTime
        /// </summary>
        public void UpdateTimeline()
        {
            Timeline.Children.Clear();

            int firstPixel = (int)Math.Round(MsToPixel(timePosition)) - timelineLeftMargin; // Index of the first pixel to draw if the timeline starts at 0, taking into account the margin

            int firstGraduation = (int)(Math.Floor((float)(firstPixel / pixelsPerGrad)) * pixelsPerGrad); // Index of the pixel of the first graduation if the timeline starts at 0. Value <= 0.
            int lastGraduation = (int)(Math.Ceiling((firstPixel + Timeline.ActualWidth) / pixelsPerGrad) * pixelsPerGrad);

            actualTimelineEndTime = PixelToMs(firstPixel + (float)Timeline.ActualWidth);

            // Foreach graduation, draw a vertical line and a label with the time
            for (int i = firstGraduation; i < lastGraduation; i += pixelsPerGrad)
            {
                Label timeLabel = new Label()
                {
                    Content = PixelToMs(i).ToString(),
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

            UpdateTimelinePlayhead();
        }

        /// <summary>
        /// Update the playhead position on the timeline based on Static.playheadPositionInMs
        /// </summary>
        /// <param name="removeCurrentPlayhead">Delete the playhead before creating a new one?</param>
        public void UpdateTimelinePlayhead()
        {
            float positionInPixels = MsToPixel(playheadPositionInMs - timePosition) + timelineLeftMargin; //Calculate the position on the playhead, taking into account the margin

            Playhead.X1 = positionInPixels;
            Playhead.X2 = positionInPixels;
        }
        
        /// <summary>
        /// Update the canvas of each track
        /// </summary>
        public void UpdateEveryTrackCanvas()
        {
            foreach (ComposerTrack composerTrack in TracksContainer.Children)
            {
                composerTrack.currentTab.hasChanged = true;
            }
        }

        
        /// <summary>
        /// Convert a value from ms to pixels.
        /// </summary>
        internal float MsToPixel(float value)
        {
            return value * msToPixelCoeff / timeZoom;
        }

        /// <summary>
        /// Convert a value from pixels to ms.
        /// </summary>
        internal float PixelToMs(float value)
        {
            return value * pixelToMsCoeff * timeZoom;
        }

        internal float GetSoundDurationInMs()
        {
            float soundDurationInMs = 0;
            foreach (ComposerTrack composerTrack in MainWindow.composer.TracksContainer.Children)
            {
                if (composerTrack.pitchTab.pointList.Count > 0)
                {
                    soundDurationInMs = Math.Max(soundDurationInMs, composerTrack.pitchTab.pointList.Last().X);
                }
            }

            return soundDurationInMs;
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

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            clickedElement = null;
        }
        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            // Move horizontally when moving on the timeline
            if (e.LeftButton == MouseButtonState.Pressed && clickedElement == Timeline)
            {
                int deltaXInPixels = (int)(lastMousePosition.X - e.GetPosition(MainGrid).X);
                float deltaXInMs = PixelToMs(deltaXInPixels);

                if (timePosition + deltaXInMs < 0)
                {
                    timePosition = 0;
                }
                else
                {
                    timePosition += deltaXInMs;
                }

                UpdateTimeline();
                UpdateEveryTrackCanvas();

                lastMousePosition = e.GetPosition(MainGrid);
            }

            // Move vertically when moving on the vertical scale bar
            else if (e.LeftButton == MouseButtonState.Pressed && clickedElement?.Name == "VerticalScaleBar")
            {
                ComposerTrack track = clickedElement.Tag as ComposerTrack;

                int deltaYInPixels = (int)(lastMousePosition.Y - e.GetPosition(MainGrid).Y);
                float deltaYInUnit = -track.currentTab.PixelToUnit(deltaYInPixels);

                if (track.pitchTab.verticalPosition + deltaYInUnit < 0)
                {
                    track.pitchTab.verticalPosition = 0;
                }
                else
                {
                    track.pitchTab.verticalPosition = track.pitchTab.verticalPosition + deltaYInUnit;
                }

                track.currentTab.hasChanged = true;
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
        private void StereoCheckBox_LeftClick(object sender, MouseButtonEventArgs e)
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
            timePosition = GetSoundDurationInMs();

            UpdateTimeline();
            UpdateEveryTrackCanvas();
        }
        private void PlayControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // If the audio has been changed or if the sound file doesn't exist, generate a new sound file
            if (hasAudioChanged || !System.IO.File.Exists("output.wav"))
            {
                var a = new System.Media.SoundPlayer("Resources/Click.wav");
                
                audioPlayerState = audioPlayerStates.Stopped;
                audioPlayer.Stop();
                audioPlayer.Close();
                SoundGenerator.Generate();
                currentAudioPlayerDuration = GetSoundDurationInMs();
            }

            // If the audio is stopped (not started or ended), open the sound file
            if (audioPlayerState == audioPlayerStates.Stopped)
            {
                isAudioLoaded = false;
                
                audioPlayer.Open(new Uri("output.wav", UriKind.Relative));

                if (currentAudioPlayerDuration < playheadPositionInMs)
                {
                    playheadPositionInMs = 0;
                }

                audioPlayer.Position = TimeSpan.FromMilliseconds(playheadPositionInMs);

                hasAudioChanged = false;
            }

            // If the audio is paused or stopped, play the sound
            if (audioPlayerState == audioPlayerStates.Stopped || audioPlayerState == audioPlayerStates.Paused)
            {
                audioPlayerState = audioPlayerStates.Playing;
                audioPlayer.Play();

                // Update GUI
                PlayButton_PauseRect1.Opacity = 1;
                PlayButton_PauseRect2.Opacity = 1;
                PlayButton_PlayIcon.Opacity = 0;
                PlayControl_Grid.ToolTip = "Pause";
            }

            // If the audio is playing, pause the audio player
            else if (audioPlayerState == audioPlayerStates.Playing)
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
            timelineClickPosition = (float)e.GetPosition(Timeline).X;
        }
        private void Timeline_MouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            // If the mouse has not been moved horizontally, set the playhead and the audio player position
            if (e.GetPosition(Timeline).X == timelineClickPosition)
            {
                playheadPositionInMs = PixelToMs(timelineClickPosition - timelineLeftMargin + 1) + timePosition;
                audioPlayer.Position = TimeSpan.FromMilliseconds(playheadPositionInMs);
                UpdateTimelinePlayhead();
            }
        }

        #endregion

        public static class Static
        {
            public static int sampleRate = 44100;

            public const float dbPerPixel = 5; // Decibel per pixel displayed for a zoom of 1.
            


        }
    }
}
