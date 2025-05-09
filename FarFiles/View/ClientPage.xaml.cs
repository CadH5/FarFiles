namespace FarFiles;

public partial class ClientPage : ContentPage
{
    protected ClientViewModel _clientViewModel;
	public ClientPage(ClientViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
		viewModel.ContentPageRef = this;
	}

    public void ClrAll()
	{
		FfCollView.SelectedItems.Clear();
	}

    private void ClientPage_Loaded(object sender, EventArgs e)
    {
        _clientViewModel = (ClientViewModel)BindingContext;
        UpdatePage();
    }

    public void FfCollView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
        UpdatePage();
    }

	protected void UpdatePage()
	{
        int numSelected = FfCollView.SelectedItems.Count;
        BtnClrAll.IsEnabled = numSelected > 0;
        BtnCopy.IsEnabled = numSelected > 0;
        BtnGoto.IsEnabled = numSelected == 1;   //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!! must be: only dir
        LblSelectedNofN.Text = $"selected from server: {numSelected}" +
                $" of {_clientViewModel.FileOrFolderColl.Count}";
    }
}

