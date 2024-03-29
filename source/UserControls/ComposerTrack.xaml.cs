﻿using NAudio.Wave;
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
using PointF = System.Drawing.PointF;

namespace Vibrante.UserControls
{
    public partial class ComposerTrack : UserControl
    {
        internal Classes.ComposerTab pitchTab = new Classes.ComposerTab()
        {
            unitSymbol = "Hz",
            mainColor = new SolidColorBrush(Color.FromRgb(255, 85, 66)),
            verticalZoom = 1,
            verticalPosition = 0,
            unitsPerGrad = 100,
            pixelsPerGrad = 20,
            minValue = 0,
            maxValue = float.PositiveInfinity,
        };

        internal Classes.ComposerTab volumeTab = new Classes.ComposerTab()
        {
            unitSymbol = "%",
            mainColor = new SolidColorBrush(Color.FromRgb(19, 185, 75)),
            verticalZoom = 1,
            verticalPosition = 0,
            unitsPerGrad = 20,
            minValue = 0,
            maxValue = 100,
        };

        internal Classes.ComposerTab panningTab = new Classes.ComposerTab()
        {
            unitSymbol = "%",
            mainColor = new SolidColorBrush(Color.FromRgb(54, 111, 224)),
            verticalZoom = 1,
            verticalPosition = -100,
            unitsPerGrad = 20,
            minValue = -100,
            maxValue = 100,
        };

        internal Classes.ComposerTab currentTab;


        private ComboBoxItem[] interpolationComboBoxItemsSource;

        // Generation variables are used by the sound generator to store data about the current track during generation.
        internal int generationVar_CurrentPitchPointIndex = 0;
        internal double generationVar_CurrentPitchIntegral = 0;
        internal int generationVar_CurrentVolumePointIndex = 0;
        internal int generationVar_CurrentPanningPointIndex = 0;


        public ComposerTrack()
        {
            currentTab = pitchTab;
            CompositionTarget.Rendering += OnRendering;

            InitializeComponent();

            UpdateSoundList();
            UpdateInterpolationList();
        }


        /// <summary>
        /// Update the list of sounds available in the combo box.
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

        /// <summary>
        /// Update the list of interpolations available in the combo box.
        /// </summary>
        public void UpdateInterpolationList()
        {
            string pitchSelection = PitchTab_InterpolationCB.SelectedValue as string;
            string volumeSelection = VolumeTab_InterpolationCB.SelectedValue as string;
            string panningSelection = PanningTab_InterpolationCB.SelectedValue as string;

            interpolationComboBoxItemsSource = new ComboBoxItem[MainWindow.interpolationEditor.interpolations.Length];

            for (int i = 0; i < MainWindow.interpolationEditor.interpolations.Length; i++)
            {
                ComboBoxItem item = new ComboBoxItem()
                {
                    Content = MainWindow.interpolationEditor.interpolations[i].name,
                    Tag = MainWindow.interpolationEditor.interpolations[i],
                };

                interpolationComboBoxItemsSource[i] = item;
            }

            PitchTab_InterpolationCB.ItemsSource = interpolationComboBoxItemsSource;
            VolumeTab_InterpolationCB.ItemsSource = interpolationComboBoxItemsSource;
            PanningTab_InterpolationCB.ItemsSource = interpolationComboBoxItemsSource;

            if (pitchSelection == null)
                PitchTab_InterpolationCB.SelectedIndex = 0;
            else
                PitchTab_InterpolationCB.SelectedValue = pitchSelection;

            if (volumeSelection == null)
                VolumeTab_InterpolationCB.SelectedIndex = 0;
            else
                VolumeTab_InterpolationCB.SelectedValue = volumeSelection;

            if (panningSelection == null)
                PanningTab_InterpolationCB.SelectedIndex = 0;
            else
                PanningTab_InterpolationCB.SelectedValue = panningSelection;
        }

        
        /// <summary>
        /// Insert a point in the specified tab point list.
        /// Doesn't work if the point is out of bounds or if the point already exists.
        /// </summary>
        private void AddPointToTab(PointF point, Classes.ComposerTab tab)
        {
            // Check if the point is out of bounds
            if (point.Y < tab.minValue || point.Y > tab.maxValue)
                return;

            // Find at which index to insert it
            for (int i = 0; i < currentTab.pointList.Count; i++)
            {
                // If the time position of the point to add is less than the current point in the list
                if (point.X < currentTab.pointList[i].X)
                {
                    // Insert the new point before this point
                    currentTab.pointList.Insert(i, point);
                    currentTab.hasChanged = true;
                    return;
                }
                
                // If the point already exists
                else if (point == currentTab.pointList[i])
                    return;
            }

            // If the point has not been added and there have been no problems, add it
            currentTab.pointList.Add(point);
            currentTab.hasChanged = true;
            MainWindow.composer.hasAudioChanged = true;
        }

        /// <summary>
        /// Update the coordinates of the point, and change its index if necessary so that the list remains sorted.
        /// </summary>
        /// <param name="currentPointIndex">Current index of the point in the list.</param>
        private void MovePointInTab(float newTimePosition, float newValue, int currentPointIndex, Classes.ComposerTab tab)
        {
            // If the point is moved ahead of the previous point, swap the two points
            if (currentPointIndex >= 1 && newTimePosition < tab.pointList[currentPointIndex - 1].X)
            {
                tab.pointList[currentPointIndex] = tab.pointList[currentPointIndex - 1];
                tab.pointList[currentPointIndex - 1] = new PointF(newTimePosition, newValue);
                tab.clickedPointIndex = currentPointIndex - 1;
            }
            
            // If the point is moved after the previous point, swap the two points
            else if (currentPointIndex < tab.pointList.Count - 1 && newTimePosition > tab.pointList[currentPointIndex + 1].X)
            {
                tab.pointList[currentPointIndex] = tab.pointList[currentPointIndex + 1];
                tab.pointList[currentPointIndex + 1] = new PointF(newTimePosition, newValue);
                tab.clickedPointIndex = currentPointIndex + 1;
            }

            // Else just update the point
            else
            {
                tab.pointList[currentPointIndex] = new PointF(newTimePosition, newValue);
            }

            tab.hasChanged = true;
            MainWindow.composer.hasAudioChanged = true;
        }

        
        public void UpdateVerticalScaleBar()
        {
            VerticalScaleBar.Children.Clear();

            float unitsPerGradWithZoom = currentTab.unitsPerGrad * currentTab.verticalZoom;

            float firstGraduation = CommonUtils.RoundToPreviousMultiple(currentTab.verticalPosition, unitsPerGradWithZoom);
            float valueCount = unitsPerGradWithZoom * (float)VerticalScaleBar.ActualHeight / currentTab.pixelsPerGrad;
            float lastGraduation = CommonUtils.RoundToNextMultiple(currentTab.verticalPosition + valueCount, unitsPerGradWithZoom);

            int counter = 0;
            for (float i = firstGraduation; i <= lastGraduation; i += unitsPerGradWithZoom, counter++)
            {
                double pixelPosition = currentTab.UnitToPixel(i - currentTab.verticalPosition);

                Label unitLabel = new Label()
                {
                    Content = i.ToString(),
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextForeground"],
                    IsHitTestVisible = false,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FontSize = 10
                };

                unitLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity)); // Needed to center the label vertically
                Canvas.SetBottom(unitLabel, pixelPosition - unitLabel.DesiredSize.Height / 2);

                VerticalScaleBar.Children.Add(unitLabel);

                Line gradLine = new Line()
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
                Canvas.SetBottom(gradLine, pixelPosition);
                VerticalScaleBar.Children.Add(gradLine);
            }
        }

        public void UpdateCanvas(int lineSegmentLength = 5, bool enablePointTooltip = false)
        {
            Brush color = currentTab.constantValue != null ? (SolidColorBrush)Resources["DisabledColor"] : currentTab.mainColor;
            
            void DrawPoint(float x, float y, int i)
            {
                Grid pointContainer = new Grid()
                {
                    Width = Properties.Settings.Default.SoundCompositer_DotSize * 2,
                    Height = Properties.Settings.Default.SoundCompositer_DotSize * 2,
                    Background = Brushes.Transparent,
                    Margin = new Thickness(x - Properties.Settings.Default.SoundCompositer_DotSize, y - Properties.Settings.Default.SoundCompositer_DotSize, 0, 0),
                    Children =
                    {
                        new Ellipse()
                        {
                            Fill = color,
                            Height = Properties.Settings.Default.SoundCompositer_DotSize,
                            Width = Properties.Settings.Default.SoundCompositer_DotSize,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            IsHitTestVisible = false
                        }
                    }
                };

                if (enablePointTooltip)
                {
                    pointContainer.ToolTip = "X: " + currentTab.pointList[i].X + "ms Y: " + currentTab.pointList[i].Y + currentTab.unitSymbol;
                }

                pointContainer.MouseDown += (s, ev) =>
                {
                    if (ev.LeftButton == MouseButtonState.Pressed) // Select the point
                    {
                        currentTab.clickedPointIndex = i;
                    }

                    else if (ev.RightButton == MouseButtonState.Pressed) // Remove the point on right click
                    {
                        currentTab.pointList.RemoveAt(i);
                        currentTab.hasChanged = true;
                        MainWindow.composer.hasAudioChanged = true;
                        UpdateCanvas();
                    }
                };

                pointContainer.MouseMove += (s, ev) =>
                {
                    if (ev.RightButton == MouseButtonState.Pressed) // Remove the point on mouseover with right button pressed
                    {
                        currentTab.pointList.RemoveAt(i);
                        currentTab.hasChanged = true;
                        MainWindow.composer.hasAudioChanged = true;
                        UpdateCanvas();
                    }
                };

                // Pass the MouseWheel event of the point to the MouseWheel event of the canvas if the point is selected (doesn't transmit the event on its own)
                pointContainer.MouseWheel += (s, ev) =>
                {
                    if (ev.LeftButton == MouseButtonState.Pressed && currentTab.clickedPointIndex != null)
                    {
                        CanvasMouseWheel(s, ev);
                    }
                };
                
                // Pass the MouseUp event of the point to the MouseUp event of the canvas (doesn't transmit the event on its own)
                pointContainer.MouseUp += (s, ev) =>
                {
                    CanvasMouseButtonUp(s, ev);
                };
                
                SoundCanvas.Children.Add(pointContainer);
            }
            
            void DrawLine(PointF point1, PointF point2)
            {
                // If the interpolation function has a custom line drawing function, use it
                if (currentTab.interpolation.customLineDrawingFunction != null)
                {
                    foreach (FrameworkElement element in currentTab.interpolation.customLineDrawingFunction(point1, point2, color))
                    {
                        SoundCanvas.Children.Add(element);
                    }
                }

                // Otherwise, draw the line by segments
                else
                {
                    // Number of line segments between the two points
                    int segmentsCount = (int)Math.Floor((point2.X - point1.X) / lineSegmentLength);

                    // For each segment
                    for (int i = 0; i < segmentsCount; i++)
                    {
                        float startX = point1.X + i * lineSegmentLength;
                        float startY = currentTab.interpolation.function(point1, point2, startX);
                        float endX = i < segmentsCount - 1 ? point1.X + (i + 1) * lineSegmentLength : point2.X;
                        float endY = currentTab.interpolation.function(point1, point2, endX);

                        // Draw the line
                        Line line = new Line()
                        {
                            X1 = startX,
                            Y1 = startY,
                            X2 = endX,
                            Y2 = endY,
                            Stroke = color,
                            StrokeThickness = 2,
                            IsHitTestVisible = false
                        };

                        SoundCanvas.Children.Add(line);
                    }

                    // If points are too close to draw a segment, draw a straight line
                    if (segmentsCount == 0)
                    {
                        Line line = new Line()
                        {
                            X1 = point1.X,
                            Y1 = point1.Y,
                            X2 = point2.X,
                            Y2 = point2.Y,
                            Stroke = color,
                            StrokeThickness = 2,
                            IsHitTestVisible = false
                        };

                        SoundCanvas.Children.Add(line);
                    }
                }

            }

            SoundCanvas.Children.Clear();
            
            
            int index = 0; // Index of the current point
            float previousX = 0; // X coordinate of the previous point
            float previousY = 0; // Y coordinate of the previous point

            foreach (PointF point in currentTab.pointList)
            {
                // Coordinates in pixels of the point
                float x = MainWindow.composer.MsToPixel(point.X - MainWindow.composer.timePosition);
                float y = (float)SoundCanvas.ActualHeight - currentTab.UnitToPixel(point.Y - currentTab.verticalPosition);
                
                DrawPoint(x, y, index);

                // If the current point is not the first, draw a line between this point and the previous one
                if (index > 0)
                {
                    DrawLine(new PointF(previousX, previousY), new PointF(x, y));
                }

                // Update the previous point coordinates
                previousX = x;
                previousY = y;

                index++;
            }

            // If the value is constant, draw a line at the constant value
            if (currentTab.constantValue != null)
            {
                double y = SoundCanvas.ActualHeight - currentTab.UnitToPixel(currentTab.constantValue.Value - currentTab.verticalPosition);

                SoundCanvas.Children.Add(new Line()
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = SoundCanvas.ActualWidth,
                    Y2 = y,
                    Stroke = currentTab.mainColor,
                    StrokeThickness = 2,
                    IsHitTestVisible = false
                });
            }
            
            // If the current tab is the volume or the panning tab, draw hoizontal lines before the first point and after the last point
            if (currentTab == volumeTab || currentTab == panningTab)
            {
                if (currentTab.pointList.Count >= 1)
                {
                    PointF firstPoint = currentTab.pointList.First();
                    PointF lastPoint = currentTab.pointList.Last();

                    double x1 = MainWindow.composer.MsToPixel(firstPoint.X - MainWindow.composer.timePosition);
                    double y1 = SoundCanvas.ActualHeight - currentTab.UnitToPixel(firstPoint.Y - currentTab.verticalPosition);
                    double x2 = MainWindow.composer.MsToPixel(lastPoint.X - MainWindow.composer.timePosition);
                    double y2 = SoundCanvas.ActualHeight - currentTab.UnitToPixel(lastPoint.Y - currentTab.verticalPosition);

                    SoundCanvas.Children.Add(new Line()
                    {
                        X1 = 0,
                        Y1 = y1,
                        X2 = x1,
                        Y2 = y1,
                        Stroke = color,
                        StrokeThickness = 2,
                        IsHitTestVisible = false
                    });

                    SoundCanvas.Children.Add(new Line()
                    {
                        X1 = x2,
                        Y1 = y2,
                        X2 = SoundCanvas.ActualWidth,
                        Y2 = y2,
                        Stroke = color,
                        StrokeThickness = 2,
                        IsHitTestVisible = false
                    });
                }

            }

        }



        #region Events

        /// <summary>
        /// Called before each UI render.
        /// <br>If the current tab has been modified since the last render, update the canvas.</br>
        /// </summary>
        private void OnRendering(object sender, EventArgs e)
        {
            if (currentTab.hasChanged)
            {
                UpdateCanvas();
                currentTab.hasChanged = false;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Calculate the number of pixels per graduation required to display all values in the vertical scale bar
            volumeTab.pixelsPerGrad = (float)VerticalScaleBar.ActualHeight * (volumeTab.unitsPerGrad / (volumeTab.maxValue - volumeTab.minValue));
            panningTab.pixelsPerGrad = (float)VerticalScaleBar.ActualHeight * (panningTab.unitsPerGrad / (panningTab.maxValue - panningTab.minValue));

            pitchTab.UpdateCoefficients();
            volumeTab.UpdateCoefficients();
            panningTab.UpdateCoefficients();

            UpdateVerticalScaleBar();
        }
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //UpdateCanvas();
            //UpdateVerticalScaleBar();
        }

        private void TrackName_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            // Select all the text in the textbox when double-clicking on it
            if (e.ClickCount == 2)
            {
                ((TextBox)sender).SelectAll();
            }
        }
        private void TrackName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // If the user presses the Enter key, unfocus the textbox
            if (e.Key == Key.Enter)
            {
                MainWindow.keyboardfocus.Focus();
            }
        }
        
        private void DeleteTrack_LeftClick(object sender, MouseButtonEventArgs e)
        {
            MainWindow.composer.TracksContainer.Children.Remove(this);
            MainWindow.composer.hasAudioChanged = true;
        }

        private void PitchTab_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            TabController.SelectedIndex = 0;
            currentTab = pitchTab;

            StereoDisabledAlert_Grid.Visibility = Visibility.Hidden;

            UpdateCanvas();
            UpdateVerticalScaleBar();
        }
        private void VolumeTab_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            TabController.SelectedIndex = 1;
            currentTab = volumeTab;
            
            StereoDisabledAlert_Grid.Visibility = Visibility.Hidden;

            UpdateCanvas();
            UpdateVerticalScaleBar();
        }
        private void PanningTab_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            TabController.SelectedIndex = 2;
            currentTab = panningTab;

            // Update the visibility of the stereo disabled alert
            StereoDisabledAlert_Grid.Visibility = MainWindow.composer.stereoEnabled ? Visibility.Hidden : Visibility.Visible;

            UpdateCanvas();
            UpdateVerticalScaleBar();
        }

        private void PitchTab_CopyPointsClick(object sender, RoutedEventArgs e)
        {
            MainWindow.composer.copiedPoints = new List<PointF>(pitchTab.pointList);
        }
        private void VolumeTab_CopyPointsClick(object sender, RoutedEventArgs e)
        {
            MainWindow.composer.copiedPoints = new List<PointF>(volumeTab.pointList);
        }
        private void PanningTab_CopyPointsClick(object sender, RoutedEventArgs e)
        {
            MainWindow.composer.copiedPoints = new List<PointF>(panningTab.pointList);
        }

        private void PitchTab_PasteAddPointsClick(object sender, RoutedEventArgs e)
        {
            Tab_PasteAddPoints(pitchTab);
        }
        private void VolumeTab_PasteAddPointsClick(object sender, RoutedEventArgs e)
        {
            Tab_PasteAddPoints(volumeTab);
        }
        private void PanningTab_PasteAddPointsClick(object sender, RoutedEventArgs e)
        {
            Tab_PasteAddPoints(panningTab);
        }
        private void Tab_PasteAddPoints(Classes.ComposerTab tab)
        {
            if (MainWindow.composer.copiedPoints.Count == 0) return; // If there are no points to paste, return

            foreach (PointF point in MainWindow.composer.copiedPoints)
            {
                AddPointToTab(point, tab);
            }
        }
        
        private void PitchTab_PasteReplacePointsClick(object sender, RoutedEventArgs e)
        {
            Tab_PasteReplacePoints(pitchTab);
        }
        private void VolumeTab_PasteReplacePointsClick(object sender, RoutedEventArgs e)
        {
            Tab_PasteReplacePoints(volumeTab);
        }
        private void PanningTab_PasteReplacePointsClick(object sender, RoutedEventArgs e)
        {
            Tab_PasteReplacePoints(panningTab);
        }
        private void Tab_PasteReplacePoints(Classes.ComposerTab tab)
        {
            if (MainWindow.composer.copiedPoints.Count == 0) return; // If there are no points to paste, return

            tab.pointList.Clear();
            
            foreach (PointF point in MainWindow.composer.copiedPoints)
            {
                AddPointToTab(point, tab);
            }
        }

        private void PitchTab_InterpolationCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pitchTab.interpolation = (Classes.Interpolation)((ComboBoxItem)PitchTab_InterpolationCB.SelectedItem).Tag;
            pitchTab.hasChanged = true;
            MainWindow.composer.hasAudioChanged = true;
        }
        
        private void VolumeTab_ConstantCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Enable the textbox only if the checkbox is checked
            if (VolumeTab_ConstantTextBox != null)
            {
                VolumeTab_ConstantTextBox.IsEnabled = VolumeTab_ConstantCheckBox.IsChecked == true;
            }
            
            
            if (VolumeTab_ConstantCheckBox.IsChecked == true)
            {
                volumeTab.constantValue = VolumeTab_ConstantTextBox?.Value;
            }
            else
            {
                volumeTab.constantValue = null;
            }

            volumeTab.hasChanged = true;
            MainWindow.composer.hasAudioChanged = true;
        }
        private void VolumeTab_ConstantTextBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            volumeTab.constantValue = VolumeTab_ConstantTextBox.Value;

            volumeTab.hasChanged = true;
            MainWindow.composer.hasAudioChanged = true;
        }
        private void VolumeTab_InterpolationCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            volumeTab.interpolation = (Classes.Interpolation)((ComboBoxItem)VolumeTab_InterpolationCB.SelectedItem).Tag;
            volumeTab.hasChanged = true;
            MainWindow.composer.hasAudioChanged = true;
        }

        private void PanningTab_ConstantCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (PanningTab_ConstantTextBox != null)
            {
                PanningTab_ConstantTextBox.IsEnabled = PanningTab_ConstantCheckBox.IsChecked == true;
            }


            if (PanningTab_ConstantCheckBox.IsChecked == true)
            {
                panningTab.constantValue = PanningTab_ConstantTextBox?.Value;
            }
            else
            {
                panningTab.constantValue = null;
            }

            panningTab.hasChanged = true;
            MainWindow.composer.hasAudioChanged = true;
        }
        private void PanningTab_ConstantTextBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            panningTab.constantValue = PanningTab_ConstantTextBox.Value;

            panningTab.hasChanged = true;
            MainWindow.composer.hasAudioChanged = true;
        }
        private void PanningTab_InterpolationCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            panningTab.interpolation = (Classes.Interpolation)((ComboBoxItem)PanningTab_InterpolationCB.SelectedItem).Tag;
            panningTab.hasChanged = true;
            MainWindow.composer.hasAudioChanged = true;
        }
        
        private void CanvasMouseLeftClick(object sender, MouseButtonEventArgs e)
        {
            // Add a point
            if (SoundCanvas.IsMouseDirectlyOver)
            {
                float positionInMs = (float)Math.Round(MainWindow.composer.PixelToMs((float)e.GetPosition(SoundCanvas).X) + MainWindow.composer.timePosition, 2);
                float positionInUnit = (float)Math.Round(currentTab.PixelToUnit((float)(SoundCanvas.ActualHeight - e.GetPosition(SoundCanvas).Y)) + currentTab.verticalPosition, 2);

                if (MainWindow.composer.snapToGridEnabled)
                {
                    positionInMs = CommonUtils.RoundToNearestMultiple(positionInMs, MainWindow.composer.SnapToGridSpacingValueX_TextBox.Value);
                    positionInUnit = CommonUtils.RoundToNearestMultiple(positionInUnit, MainWindow.composer.SnapToGridSpacingValueY_TextBox.Value);
                }

                AddPointToTab(new PointF(positionInMs, positionInUnit), currentTab);

                currentTab.clickedPointIndex = null;
            }
        }
        
        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            PointF mousePosition = new PointF((float)Math.Round(e.GetPosition(SoundCanvas).X, 0), (float)Math.Round(e.GetPosition(SoundCanvas).Y, 0));
            
            //Update PositionLabel
            float positionInMs = (float)Math.Round(MainWindow.composer.PixelToMs(mousePosition.X) + MainWindow.composer.timePosition, 2);
            float positionInUnit = (float)Math.Round(currentTab.PixelToUnit((float)SoundCanvas.ActualHeight - mousePosition.Y) + currentTab.verticalPosition, 2);
            
            if (MainWindow.composer.snapToGridEnabled)
            {
                positionInMs = CommonUtils.RoundToNearestMultiple(positionInMs, MainWindow.composer.SnapToGridSpacingValueX_TextBox.Value);
                positionInUnit = CommonUtils.RoundToNearestMultiple(positionInUnit, MainWindow.composer.SnapToGridSpacingValueY_TextBox.Value);
            }

            PositionLabel.Content = "X: " + positionInMs + "ms Y: " + positionInUnit + currentTab.unitSymbol;


            //Move a point
            if (e.LeftButton == MouseButtonState.Pressed && currentTab.clickedPointIndex != null)
            {
                MovePointInTab(positionInMs, positionInUnit, (int)currentTab.clickedPointIndex, currentTab);
            }
        }

        private void CanvasMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            currentTab.clickedPointIndex = null;
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
            MainWindow.composer.clickedElement = VerticalScaleBar;
            MainWindow.composer.lastMousePosition = e.GetPosition(MainWindow.composer.MainGrid);
        }
        
        private void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Disable zooming and panning for tabs other than "Pitch" (because of a rounding bug with bounds that I can't solve)
            if (currentTab != pitchTab)
            {
                return;
            }
            
            bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl);
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift);
            
            // Ctrl + Wheel => Vertical Zoom
            if (isCtrlPressed && !isShiftPressed)
            {
                float mousePositionInPixels = (float)(SoundCanvas.ActualHeight - e.GetPosition(SoundCanvas).Y);
                float mousePositionInUnitBefore = currentTab.PixelToUnit(mousePositionInPixels) + currentTab.verticalPosition;
                
                float divider = e.Delta < 0 ? 0.5f : 2f;

                if (currentTab.verticalZoom / divider >= 0.01)
                {
                    currentTab.verticalZoom /= divider;
                }
                    
                
                float mousePositionInUnitAfter = currentTab.PixelToUnit(mousePositionInPixels) + currentTab.verticalPosition;

                currentTab.verticalPosition += (mousePositionInUnitBefore - mousePositionInUnitAfter);
                currentTab.verticalPosition = (currentTab.verticalPosition < 0) ? 0 : currentTab.verticalPosition;
                
                UpdateVerticalScaleBar();
            }

            // Ctrl + Shift + Wheel => Time Zoom
            else if (isCtrlPressed && isShiftPressed)
            {
                float mousePositionInPixels = (float)e.GetPosition(SoundCanvas).X;
                float mousePositionInMsBefore = MainWindow.composer.PixelToMs(mousePositionInPixels);

                float divider = e.Delta < 0 ? 0.5f : 2f;

                if (MainWindow.composer.timeZoom / divider >= 0.01)
                {
                    MainWindow.composer.timeZoom /= divider;
                }

                float mousePositionInMsAfter = MainWindow.composer.PixelToMs(mousePositionInPixels);

                MainWindow.composer.timePosition += (mousePositionInMsBefore - mousePositionInMsAfter);
                MainWindow.composer.timePosition = (MainWindow.composer.timePosition < 0) ? 0 : MainWindow.composer.timePosition;

                MainWindow.composer.UpdateTimeline();
                MainWindow.composer.UpdateEveryTrackCanvas();
            }

            // Shift + Wheel => Time Panning
            else if (isShiftPressed)
            {
                float delta = (MainWindow.composer.msPerGrad / 4) * MainWindow.composer.timeZoom * Math.Sign(e.Delta);

                MainWindow.composer.timePosition = Math.Max(0, MainWindow.composer.timePosition + delta);

                MainWindow.composer.UpdateTimeline();
                MainWindow.composer.UpdateEveryTrackCanvas();
            }

            // Only Wheel => Vertical Panning
            else
            {
                float delta = (currentTab.unitsPerGrad / 2) * currentTab.verticalZoom * Math.Sign(e.Delta);

                // Clamp the position between the min and max values (max not implemented due to a rounding bug that I can't solve)
                currentTab.verticalPosition = Math.Max(currentTab.minValue, currentTab.verticalPosition + delta);

                UpdateVerticalScaleBar();
            }

            e.Handled = true;

            CanvasMouseMove(sender, e); // Simulate mouse movement to update mouse position

            currentTab.hasChanged = true;
        }


        #endregion


    }
}
