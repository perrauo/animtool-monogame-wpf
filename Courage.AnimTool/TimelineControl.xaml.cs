using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Courage.AnimTool
{
	public partial class TimelineControl : Grid
	{
		private double _framerate;
		private int _frames;
		private double _playheadPosition;

		public TimelineControl()
		{
			InitializeComponent();
			FramerateTextBox.TextChanged += FramerateTextBox_TextChanged;
			FramesTextBox.TextChanged += FramesTextBox_TextChanged;
			TimelineCanvas.MouseLeftButtonDown += TimelineCanvas_MouseLeftButtonDown;
			TimelineCanvas.MouseMove += TimelineCanvas_MouseMove;
			TimelineCanvas.MouseLeftButtonUp += TimelineCanvas_MouseLeftButtonUp;

			// Set initial values for framerate and frames
			_framerate = double.TryParse(FramerateTextBox.Text, out double framerate) ? framerate : 24;
			_frames = int.TryParse(FramesTextBox.Text, out int frames) ? frames : 100;

			// Update the timeline with the initial values
			UpdateTimeline();
		}

		private void FramerateTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if(double.TryParse(FramerateTextBox.Text, out double framerate))
			{
				_framerate = framerate;
				UpdateTimeline();
			}
		}

		private void FramesTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if(int.TryParse(FramesTextBox.Text, out int frames))
			{
				// Ensure there's at least one frame and prevent input of frame one
				if(frames < 1)
				{
					MessageBox.Show("Number of frames must be at least 1.");
					FramesTextBox.Text = "1";
					return;
				}

				_frames = frames;
				UpdateTimeline();
			}
		}

		private void TimelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if(e.LeftButton == MouseButtonState.Pressed)
			{
				MovePlayhead(e.GetPosition(TimelineCanvas).X);
			}
		}

		private void TimelineCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if(e.LeftButton == MouseButtonState.Pressed)
			{
				MovePlayhead(e.GetPosition(TimelineCanvas).X);
			}
		}

		private void TimelineCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// Handle mouse button release if needed
		}

		private void MovePlayhead(double position)
		{
			_playheadPosition = position;
			Playhead.X1 = _playheadPosition;
			Playhead.X2 = _playheadPosition;
		}

		private void UpdateTimeline()
		{
			// Ensure the TimelineCanvas has been rendered
			if(TimelineCanvas.ActualWidth == 0)
			{
				TimelineCanvas.Loaded += (s, e) => UpdateTimeline();
				return;
			}

			// Update the timeline based on the framerate and number of frames
			FrameNumbers.Items.Clear();
			double tickSpacing = TimelineCanvas.ActualWidth / _frames; // Adjusted calculation
			for(int i = 0; i <= _frames; i++)
			{
				double position = i * tickSpacing;
				StackPanel framePanel = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center };
				Line tickMark = new Line { X1 = 0, Y1 = 0, X2 = 0, Y2 = 10, Stroke = Brushes.Black, StrokeThickness = 1 };
				TextBlock frameNumber = new TextBlock { Text = i.ToString(), Foreground = Brushes.Black };
				framePanel.Children.Add(tickMark);
				framePanel.Children.Add(frameNumber);
				FrameNumbers.Items.Add(framePanel); // Add the framePanel to the ItemsControl
				Canvas.SetLeft(framePanel, position); // Set the position
			}
		}
	}
}
