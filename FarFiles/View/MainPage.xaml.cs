namespace FarFiles.View;

public partial class MainPage : ContentPage
{
	public MainPage(FilesViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}

