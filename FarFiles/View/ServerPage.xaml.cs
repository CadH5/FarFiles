namespace FarFiles;

public partial class ServerPage : ContentPage
{
	public ServerPage(FilesViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}

