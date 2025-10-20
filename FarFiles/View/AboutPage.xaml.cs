namespace FarFiles;

public partial class AboutPage : ContentPage
{
	public AboutPage()
	{
		InitializeComponent();
        lblVersion.Text = $"Version {AppInfo.Current.VersionString}";
    }

    private async void OnLinkTapped(object sender, TappedEventArgs e)
    {
        var url = "https://www.cadh5.com/farfiles";
        await Launcher.Default.OpenAsync(url);
    }
}