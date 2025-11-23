namespace FarFiles;

[QueryProperty(nameof(Info), "Info")]

public partial class AdvancedPage : ContentPage
{
    public string Info { get; set; }
    public AdvancedPage(AdvancedViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // JWdP 20251123 InitializeComponent() should not be necessary but I have found this to
        // resolve a stupid ... I think bug, in the Editor control that displays Info:
        // first time displays all right, with its newlines. Then after user goes back to MainPage
        // and then another time presses Advanced, the editor only displays first line! (at least in Windows).
        // (And if user makes window SMALLER (not wider), the other lines appear!)
        InitializeComponent();

        if (BindingContext is AdvancedViewModel vm)
        {
            vm.OnPageAppearing();
        }
    }
}