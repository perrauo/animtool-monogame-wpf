using System.Windows;

namespace Courage.AnimTool;

public partial class MainWindow : Window
{
	[System.Runtime.InteropServices.DllImport("nvapi64.dll", EntryPoint = "fake")]
	private static extern int LoadNvApi64();

	[System.Runtime.InteropServices.DllImport("nvapi.dll", EntryPoint = "fake")]
	private static extern int LoadNvApi32();

	//FloatingPanel _floatingWindow;

	void TryForceHighPerformanceGpu()
	{
		try
		{
			if(System.Environment.Is64BitProcess)
				LoadNvApi64();
			else
				LoadNvApi32();
		}
		catch { }
	}

	public MainWindow()
    {
		TryForceHighPerformanceGpu();

		InitializeComponent();

		//_floatingWindow = new FloatingWindow();
		//_floatingWindow.StateChanged += (s, e) =>
		//{
		//	_floatingWindow.Owner = this;
		//	_floatingWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		//};
		//_floatingWindow.Show();
	}
}
