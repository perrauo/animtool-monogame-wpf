using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System;
//using System.Drawing;
using Point = System.Windows.Point;
using System.Windows.Shapes;
using System.Windows.Media;

namespace Courage.MonoSkelly
{
	public partial class SplineEditor : Grid
	{
		private bool _isUnfolded;
		public bool IsUnfolded => _isUnfolded;

		private List<Point> _controlPoints;
		private Ellipse _selectedPoint;
		private int _selectedPointIndex; // Add this line

		public SplineEditor()
		{
			InitializeComponent();
			DisclosureToggleButton.Click += UpdateUnfoldButton;
			_controlPoints = new List<Point>();
			_selectedPointIndex = -1; // Initialize the index
		}

		private void UpdateUnfoldButton(object sender, RoutedEventArgs e)
		{
			_isUnfolded = !_isUnfolded;
			DisclosureToggleButton.Content = _isUnfolded ? "▼" : "▶";
		}

		private void SplineSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Handle spline selection change
			_controlPoints.Clear();
			SplineCanvas.Children.Clear();

			// Add initial control points for the selected spline
			_controlPoints.Add(new Point(100, 100));
			_controlPoints.Add(new Point(200, 200));
			_controlPoints.Add(new Point(300, 100));

			DrawSpline();
		}

		private void SplineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Point clickPosition = e.GetPosition(SplineCanvas);

			// Check if a control point is clicked
			for(int i = 0; i < _controlPoints.Count; i++)
			{
				var point = _controlPoints[i];
				if(Math.Abs(point.X - clickPosition.X) < 10 && Math.Abs(point.Y - clickPosition.Y) < 10)
				{
					_selectedPoint = new Ellipse
					{
						Width = 10,
						Height = 10,
						Fill = Brushes.Red
					};
					Canvas.SetLeft(_selectedPoint, point.X - 5);
					Canvas.SetTop(_selectedPoint, point.Y - 5);
					SplineCanvas.Children.Add(_selectedPoint);
					_selectedPointIndex = i; // Store the index
					return;
				}
			}

			// Add a new control point if none is selected
			_controlPoints.Add(clickPosition);
			DrawSpline();
		}

		private void SplineCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if(_selectedPoint != null && e.LeftButton == MouseButtonState.Pressed)
			{
				Point newPosition = e.GetPosition(SplineCanvas);
				Canvas.SetLeft(_selectedPoint, newPosition.X - 5);
				Canvas.SetTop(_selectedPoint, newPosition.Y - 5);

				// Update the control point position using the stored index
				_controlPoints[_selectedPointIndex] = newPosition;

				DrawSpline();
			}
		}

		private void DrawSpline()
		{
			SplineCanvas.Children.Clear();

			// Draw control points
			foreach(var point in _controlPoints)
			{
				Ellipse controlPoint = new Ellipse
				{
					Width = 10,
					Height = 10,
					Fill = Brushes.Blue
				};
				Canvas.SetLeft(controlPoint, point.X - 5);
				Canvas.SetTop(controlPoint, point.Y - 5);
				SplineCanvas.Children.Add(controlPoint);
			}

			// Draw spline (simple polyline for demonstration)
			Polyline spline = new Polyline
			{
				Stroke = Brushes.Black,
				StrokeThickness = 2
			};
			foreach(var point in _controlPoints)
			{
				spline.Points.Add(point);
			}
			SplineCanvas.Children.Add(spline);
		}
	}
}
