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

        if (BindingContext is AdvancedViewModel vm)
            vm.OnPageAppearing();
    }
}