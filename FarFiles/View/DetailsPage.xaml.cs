namespace FarFiles;

public partial class DetailsPage : ContentPage
{
	public DetailsPage(FileDetailsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}