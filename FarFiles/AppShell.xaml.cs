namespace FarFiles;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(AdvancedPage), typeof(AdvancedPage));
	}
}