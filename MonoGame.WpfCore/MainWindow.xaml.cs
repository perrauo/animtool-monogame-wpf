using System.Windows;

namespace MonoGame.WpfCore;



public partial class MainWindow : Window
{
	[System.Runtime.InteropServices.DllImport("nvapi64.dll", EntryPoint = "fake")]
	private static extern int LoadNvApi64();

	[System.Runtime.InteropServices.DllImport("nvapi.dll", EntryPoint = "fake")]
	private static extern int LoadNvApi32();

	void TryForceHighPerformanceGpu()
	{
		try
		{
			if(System.Environment.Is64BitProcess)
				LoadNvApi64();
			else
				LoadNvApi32();
		}
		catch { } // this will always be triggered, so just catch it and do nothing :P
	}

	public MainWindow()
    {
		TryForceHighPerformanceGpu();

		InitializeComponent();
    }
}
