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
    //IConnectivity connectivity;
    //JEEWEE
    //IGeolocation geolocation;
    //public FilesViewModel(FileDataService fileDataService, IConnectivity connectivity, IGeolocation geolocation)
    public ClientViewModel(FileDataService fileDataService, IConnectivity connectivity)
    {
        Title = "Far Away Files Access";
        this.fileDataService = fileDataService;
        //JEEWEE
        //this.connectivity = connectivity;
        //this.geolocation = geolocation;

        foreach (FileOrFolderData fo in MauiProgram.Info.RootFolders)
            FileOrFolderColl.Add(fo);
        foreach (FileOrFolderData fi in MauiProgram.Info.RootFiles)
            FileOrFolderColl.Add(fi);
    }

    
    public string TxtLocalRoot
    {
        get => $"Local path: {MauiProgram.Settings.FullPathRoot}";
    }

    [RelayCommand]
    async Task ClrAll()
    {
        ContentPageRef.ClrAll();
    }

    [ObservableProperty]
    bool isRefreshing;

    [RelayCommand]
    async Task GetMonkeysAsync()
    {
        if (IsBusy)
            return;

        try
        {
            //JEEWEE
            //if (connectivity.NetworkAccess != NetworkAccess.Internet)
            //{
            //    await Shell.Current.DisplayAlert("No connectivity!",
            //        $"Please check internet and try again.", "OK");
            //    return;
            //}

            IsBusy = true;
            //JEEWEETODO: BROWSER
            FileOrFolderColl.Clear();

            //JEEWEE
            //var folderPickerResult = await FolderPicker.PickAsync("");
            //if (! folderPickerResult.IsSuccessful)
            //{
            //    throw new Exception($"FolderPicker not successful or cancelled");
            //}

            //string rootPath = folderPickerResult.Folder?.Path;

            //foreach (var fileData in fileDataService.GetFilesData(rootPath))
            //{
            //    FileOrFolderColl.Add(fileData);
            //}
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get filesdata: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
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
