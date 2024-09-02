using System.Windows;
using System.Windows.Controls.Primitives;

namespace Courage.MonoSkelly
{
	public partial class LoadFileWindow : Window
	{
		public LoadFileWindow()
		{
			InitializeComponent();
			CancelButton.Click += (e, sender) =>
			{
				Close();
			};
			LoadButton.Click += LoadButton_Click; // Add this line
		}

		private void LoadButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true; // Set the dialog result to true to close the window and return true from ShowDialog()
		}
	}
}
