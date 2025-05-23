//JEEWEE
//using Android.Widget;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;

using FarFiles.Services;
using Microsoft.Maui.Controls.Internals;
using System.Threading;

namespace FarFiles.ViewModel;

public partial class ClientViewModel : BaseViewModel
{
    public ClientPage ContentPageRef;
    public ObservableCollection<Model.FileOrFolderData> FileOrFolderColl { get; } = new();
    FileDataService fileDataService;
    //JEEWEE
    //IConnectivity connectivity;
    //IGeolocation geolocation;
    //public FilesViewModel(FileDataService fileDataService, IConnectivity connectivity, IGeolocation geolocation)
    public ClientViewModel(FileDataService fileDataService, IConnectivity connectivity)
    {
        Title = "Far Away Files Access";
        this.fileDataService = fileDataService;
        //JEEWEE
        //this.connectivity = connectivity;
        //this.geolocation = geolocation;

        UpdateCollView();
    }


    protected void UpdateCollView()
    {
        FileOrFolderColl.Clear();

        if (MauiProgram.Info.SvrPathParts.Count > 0)
            FileOrFolderColl.Add(new FileOrFolderData("..", true, 0));

        foreach (FileOrFolderData fo in MauiProgram.Info.CurrSvrFolders)
            FileOrFolderColl.Add(fo);
        foreach (FileOrFolderData fi in MauiProgram.Info.CurrSvrFiles)
            FileOrFolderColl.Add(fi);

        //JEEWEE
        ContentPageRef?.DoWeird(FileOrFolderColl);        // otherwise sometimes items in new contents seem selected
    }


    public string TxtLocalRoot
    {
        get => $"Local path: {MauiProgram.Settings.FullPathRoot}";
    }

    public string TxtSvrPath
    {
        get => $"Sub path on server: '{String.Join('/', MauiProgram.Info.SvrPathParts)}'";
    }

    [RelayCommand]
    async Task ClrAll()
    {
        ContentPageRef.ClrAll();
    }

    //JEEWEE
    //[ObservableProperty]
    //bool isRefreshing;

    

    [RelayCommand]
    async Task GotoDirAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            FileOrFolderData[] selecteds = ContentPageRef.GetSelecteds();
            if (selecteds.Length != 1 || ! selecteds.First().IsDir)      // button should be disabled
                throw new Exception(
                    $"PROGRAMMERS: GotoDirAsync: num selecteds ({selecteds.Length}) not 1" +
                    " or selected is not dir");

            FileOrFolderData gotoDir = selecteds.First();

            int numSvrPartsGoto = MauiProgram.Info.SvrPathParts.Count;
            if (gotoDir.Name == "..")
            {
                if (MauiProgram.Info.SvrPathParts.Count > 0)    // should be
                    MauiProgram.Info.SvrPathParts.RemoveAt(
                                MauiProgram.Info.SvrPathParts.Count - 1);
            }
            else
            {
                MauiProgram.Info.SvrPathParts.Add(gotoDir.Name);
            }
            OnPropertyChanged("TxtSvrPath");

            await MauiProgram.Info.MainPageVwModel.SndFromClientRecievePathInfo_msgbxs_Async();
            UpdateCollView();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to goto dir: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            //JEEWEE
            //IsRefreshing = false;
        }
    }



    [RelayCommand]
    async Task CopyAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            FileOrFolderData[] selecteds = ContentPageRef.GetSelecteds();
            await MauiProgram.Info.MainPageVwModel.CopyFromSvr_msgbxs_Async(selecteds);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get filesdata: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            //JEEWEE
            //IsRefreshing = false;
        }

    }

    //JEEWEE: AWAY
    //[RelayCommand]
    //async Task GetClosestMonkey()
    //{
    //    if (IsBusy || FileOrFolderColl.Count == 0)
    //        return;

    //    try
    //    {
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Unable to query location: {ex.Message}");
    //        await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
    //    }
    //}
}
