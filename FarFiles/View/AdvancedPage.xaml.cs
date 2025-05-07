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
}