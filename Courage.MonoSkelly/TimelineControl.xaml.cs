using Microsoft.Xna.Framework;

using MonoSkelly.Core;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Courage.MonoSkelly
{
	public partial class TimelineControl : Grid
	{
		private double _framerate;
		public double Framerate
		{
			get => _framerate;
			set
			{
				_framerate = value;
			}
		}

		private int _frames;
		public int Frames
		{
			get => _frames;
			set
			{
				FramesTextBox.Text = value.ToString();
				UpdateTimeline();
			}
		}

		private bool _isPlaying;
		public bool IsPlaying => _isPlaying;

		private bool _isRepeating;
		public bool IsRepeating => _isRepeating;

		private bool _isUnfolded;
		public bool IsUnfolded => _isUnfolded;

		private Skeleton _skeleton;

		public Action<float> OnPlaybackFrame;

		public Action<string> OnAnimationSelected;

		private double _currentFrame; // Use this field to track the current frame

		public double CurrentFrame
		{
			get => _currentFrame;
			set
			{
				_currentFrame = value;
				UpdatePlayheadPosition();
			}
		}

		public TimelineControl()
		{
			InitializeComponent();

			AnimationSelector.SelectionChanged += OnAnimationSelectionChanged;
			FramerateTextBox.TextChanged += FramerateTextBox_TextChanged;
			DisclosureToggleButton.Click += UpdateUnfoldButton;

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

		public void Update(GameTime gameTime)
		{
			if(_isPlaying)
			{
				double deltaTime = gameTime.ElapsedGameTime.TotalSeconds;
				CurrentFrame += _framerate * deltaTime;

				if(CurrentFrame >= _frames)
				{
					if(_isRepeating)
					{
						CurrentFrame = 0;
						OnPlaybackFrame.Invoke(0f);
					}
					else
					{
						CurrentFrame = 0; // Reset the current frame
						Pause(); // Pause the animation
					}
				}
			}
		}

		private void Play()
		{
			_isPlaying = true;
			PlayPauseButton.Content = "⏸️";
		}

		private void Pause()
		{
			_isPlaying = false;
			PlayPauseButton.Content = "▶️";
		}

		public void LoadProject(Skeleton skeleton)
		{
			_skeleton = skeleton;
		}

		public void OnAnimationChanged(in AnimationState animation)
		{
			Frames = animation.StepsCount;
		}

		void OnAnimationSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBoxItem item = (ComboBoxItem)AnimationSelector.SelectedValue;
			string anim = (string)item.Content;
			if(!string.IsNullOrEmpty(anim))
			{
				OnAnimationSelected.Invoke(anim);
			}
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
				MovePlayheadToPosition(e.GetPosition(TimelineCanvas).X);
			}
		}

		private void TimelineCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if(e.LeftButton == MouseButtonState.Pressed)
			{
				MovePlayheadToPosition(e.GetPosition(TimelineCanvas).X);
			}
		}

		private void MovePlayheadToPosition(double position)
		{
			// Calculate the new current frame based on the mouse position
			double newFrame = (position / TimelineCanvas.ActualWidth) * _frames;

			// Ensure the current frame stays within the bounds of the timeline
			newFrame = Math.Max(0, Math.Min(_frames, newFrame));

			double delta = _currentFrame - newFrame;

			_currentFrame = newFrame;

			Playhead.X1 = (_currentFrame / _frames) * TimelineCanvas.ActualWidth;
			Playhead.X2 = (_currentFrame / _frames) * TimelineCanvas.ActualWidth;

			OnPlaybackFrame.Invoke((float)delta);
		}

		private void TimelineCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// Handle mouse button release if needed
		}

		private void UpdatePlayheadPosition()
		{
			// Update the playhead position on the canvas based on the current frame
			Playhead.X1 = (CurrentFrame / _frames) * TimelineCanvas.ActualWidth;
			Playhead.X2 = (CurrentFrame / _frames) * TimelineCanvas.ActualWidth;
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

		private void RewindButton_Click(object sender, RoutedEventArgs e)
		{
			CurrentFrame = 0;
		}

		private void StepBackwardButton_Click(object sender, RoutedEventArgs e)
		{
			CurrentFrame = Math.Max(0, CurrentFrame - 1);
		}

		private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
		{
			if(_isPlaying)
			{
				Pause();
			}
			else
			{
				Play();
			}
		}

		private void StepForwardButton_Click(object sender, RoutedEventArgs e)
		{
			CurrentFrame = Math.Min(_frames, CurrentFrame + 1);
		}

		private void FastForwardButton_Click(object sender, RoutedEventArgs e)
		{
			CurrentFrame = _frames;
		}

		private void RepeatButton_Click(object sender, RoutedEventArgs e)
		{
			_isRepeating = !_isRepeating;
			UpdateRepeatButtonAppearance();
		}

		private void UpdateRepeatButtonAppearance()
		{
			if(_isRepeating)
			{
				RepeatButton.Background = Brushes.LightBlue; // Change to your desired color
			}
			else
			{
				RepeatButton.Background = Brushes.Transparent;
			}
		}

		private void UpdateUnfoldButton(object sender, RoutedEventArgs e)
		{
			_isUnfolded = !_isUnfolded;
			if(_isUnfolded)
			{
				DisclosureToggleButton.Content = "▼";
				//RepeatButton.Background = Brushes.LightBlue; // Change to your desired color
			}
			else
			{
				DisclosureToggleButton.Content = "▶";
			}
		}
	}
}
