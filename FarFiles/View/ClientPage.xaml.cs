//JEEWEE
//using static ObjCRuntime.Dlfcn;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Metadata;

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

    public FileOrFolderData[] GetSelecteds()
    {
        return FfCollView.SelectedItems.Cast<FileOrFolderData>().ToArray();
    }

    /// <summary>
    /// Sugestion from ChatGPT to resolve weird display problem
    /// </summary>
    /// <param name="fileOrFolderData"></param>
    public void DoWeird(ObservableCollection<FileOrFolderData> fileOrFolderData)
    {
        // To resolve this problem:
        // Now, my page that has the CollectionView has a problem.
        // When I change the underlying data, part of the CollectionView rows display
        // as if selected.I have noticed that this happens when the new number of rows
        // is more than the previous number, for all rows below the previous number.
        // I know that the CollectionView.SelectedItems property is empty,
        // because I display that number in a label.

        FfCollView.ItemsSource = null;
        FfCollView.ItemsSource = fileOrFolderData;
        UpdatePage();
    }
    private void ClientPage_Loaded(object sender, EventArgs e)
    {
        _clientViewModel = (ClientViewModel)BindingContext;

        //JEEWEE
        //var dummy = Task.Run(async () =>
        //{
        //    await Task.Delay(1);
        //    MainThread.BeginInvokeOnMainThread(UpdatePage);
        //});
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
        BtnGoto.IsEnabled = numSelected == 1 && GetSelecteds().First().IsDir;
        LblSelectedNofN.Text = $"selected from server: {numSelected}" +
        $" of {_clientViewModel.FileOrFolderColl.Count}";
    }
}

