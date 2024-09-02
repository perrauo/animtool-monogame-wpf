using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Courage.MonoSkelly
{
	public interface IFloatingPanel
	{
		void FloatingPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e);
		void FloatingPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e);
		void FloatingPanel_MouseMove(object sender, MouseEventArgs e);
		void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e);
	}

	public class FloatingPanelImpl : IFloatingPanel
	{
		private bool _isDragging = false;
		private Point _clickPosition;
		private TranslateTransform _transform;
		private UserControl _userControl;

		public FloatingPanelImpl(UserControl userControl)
		{
			_userControl = userControl;
			_userControl.MouseLeftButtonDown += FloatingPanel_MouseLeftButtonDown;
			_userControl.MouseLeftButtonUp += FloatingPanel_MouseLeftButtonUp;
			_userControl.MouseMove += FloatingPanel_MouseMove;
			_transform = new TranslateTransform();
			_userControl.RenderTransform = _transform;
			_userControl = userControl;
		}

		public void FloatingPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_isDragging = true;
			_clickPosition = e.GetPosition(System.Windows.Application.Current.MainWindow);
			_userControl.CaptureMouse();
		}

		public void FloatingPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			_isDragging = false;
			_userControl.ReleaseMouseCapture();
		}

		public void FloatingPanel_MouseMove(object sender, MouseEventArgs e)
		{
			if(_isDragging)
			{
				var currentPosition = e.GetPosition(Application.Current.MainWindow);
				_transform.X += currentPosition.X - _clickPosition.X;
				_transform.Y += currentPosition.Y - _clickPosition.Y;
				_clickPosition = currentPosition;
			}
		}

		public void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
		{
			double newWidth = _userControl.Width + e.HorizontalChange;
			double newHeight = _userControl.Height + e.VerticalChange;

			if(newWidth > 0)
				_userControl.Width = newWidth;

			if(newHeight > 0)
				_userControl.Height = newHeight;
		}
	}
}
