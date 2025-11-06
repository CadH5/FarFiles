//JEEWEE
//using Microsoft.Maui.Controls;
//using Microsoft.Maui.Controls.Platform;
//JEEWEE
//using Microsoft.UI.Input;
//using Microsoft.UI.Xaml;

namespace FarFiles;

public partial class AboutPage : ContentPage
{
	public AboutPage()
	{
		InitializeComponent();
        lblVersion.Text = $"Version {AppInfo.Current.VersionString}";
        imgHand.IsVisible = false;
    }

    private async void OnLinkTapped(object sender, object e)
    {
        var lbl = (Label)sender;
        var url = lbl.Text;
        await Launcher.Default.OpenAsync(url);
    }

    private void OnPointerEntered(object sender, object e)

    {
        // JWdP 20251105 It is almost impossible to change the cursor.
        // ChatGPT made me try a solution, for which I had to downgrade from .NET 9 to 8
        // and it costed me two days of work to repair (besides I could still not figure out the solution)
        // Now, as a weird substitute, I'm using this image becoming visible/invisible
        // Note: this OnPointEntered/Exited does not work on Android by finger
        var lbl = (Label)sender;
        lbl.TextColor = Colors.Violet;
        imgHand.IsVisible = true;
    }

    private void OnPointerExited(object sender, object e)
    {
        var lbl = (Label)sender;
        lbl.TextColor = Colors.Blue;
        imgHand.IsVisible = false;
    }
}