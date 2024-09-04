using Microsoft.Xna.Framework;

using MonoSkelly.Core;

using SharpDX.Direct3D9;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading; // for use in windows form only

using Line = System.Windows.Shapes.Line;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace Courage.MonoSkelly
{
	public partial class TimelineControl : Grid
	{
		private const double KeyframeMoveThreshold = 8.0;

		private const int DragDelayMilliseconds = 500;

		private double _framerate;
		public float Framerate
		{
			get => (float)_framerate;
			set
			{
				_framerate = value;
			}
		}

		private double _frames;
		private void SetFrames(double frames)
		{
			_frames = frames;
			FramesTextBox.Text = frames.ToString();
			UpdateTimeline();
		}
		public float Frames => (float)_frames;

		private bool _isPlaying;
		public bool IsPlaying => _isPlaying;

		private bool _isRepeating;
		public bool IsRepeating => _isRepeating;

		private bool _isUnfolded;
		public bool IsUnfolded => _isUnfolded;

		private Skeleton _skeleton;

		public Action<float> OnPlaybackFrame;

		public Action<string> OnAnimationSelected;

		public Action<AnimationStep, float> OnKeyframeChanged;

		private Animation _animation;

		private double _currentFrame; // Use this field to track the current frame

		public float CurrentFrame => (float)_currentFrame;

		private void SetCurrentFrame(double currentFrame)
		{
			_currentFrame = currentFrame;
			UpdatePlayheadPosition();
		}


		public float TimeStamp => (float)(_currentFrame / _framerate);

		private Rectangle _draggingKeyframe;

		public bool IsDraggingKeyframe => _draggingKeyframe != null;

		private Rectangle _selectedKeyframe;

		public bool IsKeyframeSelected => _selectedKeyframe != null;

		private Point _dragStartPoint;

		private Dictionary<Rectangle, AnimationStep> _keyframes = new Dictionary<Rectangle, AnimationStep>();

		private DispatcherTimer _dragTimer;
		

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

			_dragTimer = new DispatcherTimer();
			_dragTimer.Interval = TimeSpan.FromMilliseconds(DragDelayMilliseconds);
			_dragTimer.Tick += DragTimer_Tick;

			// Update the timeline with the initial values
			UpdateTimeline();
		}

		public void Update(GameTime gameTime)
		{
			if(_isPlaying)
			{
				double deltaTime = gameTime.ElapsedGameTime.TotalSeconds;
				SetCurrentFrame(_currentFrame + _framerate * deltaTime);

				if(CurrentFrame >= _frames)
				{
					if(_isRepeating)
					{
						SetCurrentFrame(0);
						OnPlaybackFrame.Invoke(0f);
					}
					else
					{
						SetCurrentFrame(0); // Reset the current frame
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

		private void AddKeyframe(AnimationStep step, double timestamp)
		{
			double position = (timestamp / _frames) * TimelineCanvas.ActualWidth;
			Rectangle greenBar = new Rectangle
			{
				Width = 2,
				Height = TimelineCanvas.ActualHeight,
				Fill = Brushes.Green,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			Canvas.SetLeft(greenBar, position);
			greenBar.MouseLeftButtonDown += Keyframe_MouseLeftButtonDown;
			greenBar.MouseMove += Keyframe_MouseMove;
			greenBar.MouseLeftButtonUp += Keyframe_MouseLeftButtonUp;
			TimelineCanvas.Children.Add(greenBar);
			_keyframes.Add(greenBar, step);
		}

		public void OnAnimationChanged(in AnimationState animation)
		{
			_animation = _skeleton.GetAnimation(animation.Name);
			// 
			double totalTimeInSecs = 0;
			foreach(var step in _animation.Steps)
			{
				totalTimeInSecs += step.Duration;
			}

			SetFrames(_framerate * totalTimeInSecs);

			// Remove existing green bars
			for(int i = TimelineCanvas.Children.Count - 1; i >= 0; i--)
			{
				if(TimelineCanvas.Children[i] is Rectangle rect && rect.Fill == Brushes.Green)
				{
					TimelineCanvas.Children.RemoveAt(i);
				}
			}

			// Remove keyframes
			_keyframes.Clear();

			totalTimeInSecs = 0;
			for(int i = 0; i < _animation.Steps.Count; i ++)
			{
				var step = _animation.Steps[i];
				totalTimeInSecs += step.Duration;
				AddKeyframe(step, Framerate * totalTimeInSecs);
			}
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
				Point clickPosition = e.GetPosition(TimelineCanvas);
				if(IsClickNearKeyframe(clickPosition))
				{
					// Start dragging the keyframe
					var keyframe = GetKeyframeAtPosition(clickPosition);
					StartDraggingKeyframe(keyframe, clickPosition);
					SelectKeyframe(keyframe);
				}
				else
				{
					if(IsKeyframeSelected)
					{
						// First deselect the keyframe
						StartDraggingKeyframe(null);
						SelectKeyframe(null);
					}
					else
					{
						// Move the playhead
						MovePlayheadToPosition(clickPosition.X);
					}
				}
			}
		}

		private void TimelineCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if(IsDraggingKeyframe && e.LeftButton == MouseButtonState.Pressed)
			{
				Point currentPosition = e.GetPosition(TimelineCanvas);
				double offset = currentPosition.X - _dragStartPoint.X;
				double newLeft = Canvas.GetLeft(_draggingKeyframe) + offset;
				newLeft = Math.Max(0, Math.Min(TimelineCanvas.ActualWidth - _draggingKeyframe.Width, newLeft));
				Canvas.SetLeft(_draggingKeyframe, newLeft);
				_dragStartPoint = currentPosition;
			}
			else if(e.LeftButton == MouseButtonState.Pressed)
			{
				Point currentPosition = e.GetPosition(TimelineCanvas);
				if(!IsClickNearKeyframe(currentPosition))
				{
					// Move the playhead
					MovePlayheadToPosition(currentPosition.X);
				}
			}
		}


		private bool IsClickNearKeyframe(Point clickPosition)
		{
			foreach(UIElement element in TimelineCanvas.Children)
			{
				if(element is Rectangle keyframe && keyframe.Fill == Brushes.Green)
				{
					double keyframePosition = Canvas.GetLeft(keyframe);
					if(Math.Abs(clickPosition.X - keyframePosition) <= KeyframeMoveThreshold)
					{
						return true;
					}
				}
			}
			return false;
		}

		private Rectangle GetKeyframeAtPosition(Point position)
		{
			foreach(UIElement element in TimelineCanvas.Children)
			{
				if(element is Rectangle keyframe && keyframe.Fill == Brushes.Green)
				{
					double keyframePosition = Canvas.GetLeft(keyframe);
					if(Math.Abs(position.X - keyframePosition) <= KeyframeMoveThreshold)
					{
						return keyframe;
					}
				}
			}
			return null;
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
			//FrameNumbers.
			FrameNumbers.Items.Clear();
			FrameNumbers.Items.Refresh();
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
			SetCurrentFrame(0);
		}

		private void StepBackwardButton_Click(object sender, RoutedEventArgs e)
		{
			SetCurrentFrame(Math.Max(0, CurrentFrame - 1));
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
			SetCurrentFrame(Math.Min(_frames, _currentFrame + 1));
		}

		private void FastForwardButton_Click(object sender, RoutedEventArgs e)
		{
			SetCurrentFrame(_frames);
		}

		private void RepeatButton_Click(object sender, RoutedEventArgs e)
		{
			_isRepeating = !_isRepeating;
			UpdateRepeatButtonAppearance();
		}

		private void SelectKeyframe(Rectangle keyframe)
		{
			// Deselect the previously selected keyframe
			if(_selectedKeyframe != null)
			{
				_selectedKeyframe.Fill = Brushes.Green;
				_selectedKeyframe.StrokeThickness = 0;
			}

			_selectedKeyframe = keyframe;

			if(_selectedKeyframe != null)
			{
				// Select the new keyframe
				_selectedKeyframe = keyframe;
				_selectedKeyframe.Fill = Brushes.Blue;
				_selectedKeyframe.StrokeThickness = 2;
			}
		}

		private void StartDraggingKeyframe(Rectangle keyframe, in Point point=default)
		{
			if(_draggingKeyframe != null) 
			{
				// Ensure the timer is stopped when dragging ends
				_dragTimer.Stop();
				_draggingKeyframe.ReleaseMouseCapture();
				_dragStartPoint = new Point(0, 0);
			}

			_draggingKeyframe = keyframe;

			if(_draggingKeyframe != null)
			{
				_dragStartPoint = point;
				_draggingKeyframe.CaptureMouse();
			}
		}

		private void Keyframe_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if(e.LeftButton == MouseButtonState.Pressed)
			{
				StartDraggingKeyframe(sender as Rectangle, e.GetPosition(TimelineCanvas));
				// Handle selection
				SelectKeyframe(_draggingKeyframe);
			}
		}

		private void Keyframe_MouseMove(object sender, MouseEventArgs e)
		{
			if(IsDraggingKeyframe && e.LeftButton == MouseButtonState.Pressed)
			{
				Point currentPosition = e.GetPosition(TimelineCanvas);
				double offset = currentPosition.X - _dragStartPoint.X;
				double newLeft = Canvas.GetLeft(_draggingKeyframe) + offset;
				newLeft = Math.Max(0, Math.Min(TimelineCanvas.ActualWidth - _draggingKeyframe.Width, newLeft));
				Canvas.SetLeft(_draggingKeyframe, newLeft);
				_dragStartPoint = currentPosition;

				// Reset the timer
				_dragTimer.Stop();
				_dragTimer.Start();
			}
		}

		private void DragTimer_Tick(object sender, EventArgs e)
		{
			_dragTimer.Stop();

			// Trigger the OnKeyframeChanged event
			if(_keyframes.TryGetValue(_draggingKeyframe, out AnimationStep step))
			{
				double newFrame = (Canvas.GetLeft(_draggingKeyframe) / TimelineCanvas.ActualWidth) * _frames;
				newFrame = Math.Max(0, Math.Min(_frames, newFrame));
				float timestamp = (float)(newFrame / _framerate);
				// TODO: Fix this
				//OnKeyframeChanged?.Invoke(step, timestamp);
			}
		}

		private void Keyframe_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			StartDraggingKeyframe(null);
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
