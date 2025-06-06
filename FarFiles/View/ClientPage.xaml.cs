//JEEWEE
//using static ObjCRuntime.Dlfcn;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Metadata;

namespace FarFiles;

public partial class ClientPage : ContentPage
{
    protected ClientViewModel _clientViewModel;
    protected bool _isBusy;
    protected bool _moreButtons;


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

    public void SelectAll(ObservableCollection<Model.FileOrFolderData> items)
    {
        SelectCore(items, false, "");
    }

    public void SelectFltr(ObservableCollection<Model.FileOrFolderData> items,
                    string txtSelectFltr)
    {
        SelectCore(items, true, txtSelectFltr);
    }

    protected void SelectCore(ObservableCollection<Model.FileOrFolderData> items,
                    bool useSelectFltr, string txtSelectFltr)
    {
        FfCollView.SelectedItems.Clear();
        DoWeird(items);
        string txtSelectFltrUpc = txtSelectFltr.ToUpper();
        foreach (var item in items)
        {
            if (item.IsDir)
                continue;

            if (useSelectFltr)
            {
                if (System.String.IsNullOrEmpty(txtSelectFltr))
                    continue;
                if (! item.Name.ToUpper().Contains(txtSelectFltrUpc))
                    continue;
            }
            FfCollView.SelectedItems.Add(item);
        }
    }

    public void ClrDotDotAt0()
    {
        var fDataFirst = GetSelecteds().FirstOrDefault();
        if (fDataFirst != null && fDataFirst.Name == "..")
        {
            FfCollView.SelectedItems.RemoveAt(0);
            UpdatePage();
        }
    }

    public FileOrFolderData[] GetSelecteds()
    {
        return FfCollView.SelectedItems.Cast<FileOrFolderData>().ToArray();
    }

    public void SetValuesForUpdpgDoUpd(bool isBusy, bool moreButtons)
    {
        _isBusy = isBusy;
        _moreButtons = moreButtons;
        UpdatePage();
    }

    /// <summary>
    /// Sugestion from ChatGPT to resolve weird display problem
    /// </summary>
    /// <param name="fileOrFolderData"></param>
    public void DoWeird(ObservableCollection<FileOrFolderData> fileOrFolderData)
    {
        // May 2025: to resolve the following problem:
        // Now, my page that has the CollectionView has a problem.
        // When I change the underlying data, part of the CollectionView rows display
        // as if selected.I have noticed that this happens when the new number of rows
        // is more than the previous number, for all rows below the previous number.
        // I know that the CollectionView.SelectedItems property is empty,
        // because I display that number in a label.
        //
        // 5-jun-2025 But: although it helps, it does certainly not help 100%, maybe 90%.
        // Rows keep being displayed selected. Internally things are right: label shows
        // right values for "selected N of N". But rows are displayed selected that are NOT.
        // This is specially the case for out-of-scope rows. Scroll down, then up, and now
        // different rows are displayed selected.

        FfCollView.ItemsSource = null;
        FfCollView.ItemsSource = fileOrFolderData;
        UpdatePage();
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
        bool can = !_isBusy && !_moreButtons;
        BtnClrAll.IsEnabled = can && numSelected > 0;
        BtnCopy.IsEnabled = can && numSelected > 0;
        BtnGoto.IsEnabled = can && numSelected == 1 && GetSelecteds().First().IsDir;
        LblSelectedNofN.Text = $"selected from server: {numSelected}" +
            $" of {_clientViewModel.FileOrFolderColl.Count}";
    }
}

