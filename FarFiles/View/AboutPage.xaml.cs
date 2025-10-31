//JEEWEE
//using Microsoft.Maui.Controls;
//using Microsoft.Maui.Controls.Platform;
#if WINDOWS
//JEEWEE
//using Microsoft.UI.Input;
//using Microsoft.UI.Xaml;
#endif

namespace FarFiles;

public partial class AboutPage : ContentPage
{
	public AboutPage()
	{
		InitializeComponent();
        lblVersion.Text = $"Version {AppInfo.Current.VersionString}";
    }

    private async void OnLinkTapped(object sender, Microsoft.UI.Input.TappedEventArgs e)
    {
        var url = "https://www.cadh5.com/farfiles";
        await Launcher.Default.OpenAsync(url);
    }

    private void OnPointerEntered(object sender, Microsoft.UI.Input.PointerEventArgs e)
    {
#if WINDOWS
        //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //var platformView = (FrameworkElement)((View)sender).Handler.PlatformView;
        //var cursor = InputSystemCursor.CreateFromSystemCursor(InputSystemCursorShape.Hand);
        //platformView.ProtectedCursor = cursor;
#endif
    }

    private void OnPointerExited(object sender, Microsoft.UI.Input.PointerEventArgs e)
    {
#if WINDOWS
        //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //var platformView = (FrameworkElement)((View)sender).Handler.PlatformView;
        //platformView.ProtectedCursor = null;
#endif
    }
}