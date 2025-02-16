namespace FarFiles;

public partial class ClientPage : ContentPage
{
	public ClientPage(FilesViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}

