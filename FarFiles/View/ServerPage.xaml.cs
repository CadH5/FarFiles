namespace FarFiles;

public partial class ServerPage : ContentPage
{
	public ServerPage(ClientViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}

