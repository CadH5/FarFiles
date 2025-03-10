namespace FarFiles;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState activationState)
	{
		try
		{
			return new Window(new AppShell());
        }
        catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			return null;
		}
	}
}
