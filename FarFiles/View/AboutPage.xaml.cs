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

    //JEEWEE
    //private async void OnLinkTapped(object sender, Microsoft.UI.Input.TappedEventArgs e)
    private async void OnLinkTapped(object sender, object e)
    {
        var url = "https://www.cadh5.com/farfiles";
        await Launcher.Default.OpenAsync(url);
    }

    //JEEWEE
    //private void OnPointerEntered(object sender, Microsoft.UI.Input.PointerEventArgs e)
    private void OnPointerEntered(object sender, object e)

    {
#if WINDOWS
        //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //var platformView = (FrameworkElement)((View)sender).Handler.PlatformView;
        //var cursor = InputSystemCursor.CreateFromSystemCursor(InputSystemCursorShape.Hand);
        //platformView.ProtectedCursor = cursor;
#endif
    }

    //JEEWEE
    //private void OnPointerExited(object sender, Microsoft.UI.Input.PointerEventArgs e)
    private void OnPointerExited(object sender, object e)
    {
#if WINDOWS
        //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //var platformView = (FrameworkElement)((View)sender).Handler.PlatformView;
        //platformView.ProtectedCursor = null;
#endif
    }
}