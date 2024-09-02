using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Courage.MonoSkelly
{
	public partial class AnimsPanel : UserControl, IFloatingPanel
	{
		private FloatingPanelImpl _impl;

		public AnimsPanel()
		{
			InitializeComponent();
			_impl = new FloatingPanelImpl(this);
			//floatingPanel.Parent = this;
		}

		public void FloatingPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			((IFloatingPanel)_impl).FloatingPanel_MouseLeftButtonDown(sender, e);
		}

		public void FloatingPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			((IFloatingPanel)_impl).FloatingPanel_MouseLeftButtonUp(sender, e);
		}

		public void FloatingPanel_MouseMove(object sender, MouseEventArgs e)
		{
			((IFloatingPanel)_impl).FloatingPanel_MouseMove(sender, e);
		}

		public void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
		{
			((IFloatingPanel)_impl).ResizeThumb_DragDelta(sender, e);
		}
	}
}
