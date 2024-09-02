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
	public partial class SplineEditor : Grid
	{

		private bool _isUnfolded;
		public bool IsUnfolded => _isUnfolded;


		public SplineEditor()
		{
			InitializeComponent();
			DisclosureToggleButton.Click += UpdateUnfoldButton;
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
