//JEEWEE
//using Android.Widget;
using CommunityToolkit.Maui.Storage;

using FarFiles.Services;
using System.Threading;

namespace FarFiles.ViewModel;

public partial class FilesViewModel : BaseViewModel
{
    public ObservableCollection<Model.FileData> FilesData { get; } = new();
    FileDataService fileDataService;
    IConnectivity connectivity;
    //JEEWEE
    //IGeolocation geolocation;
    //public FilesViewModel(FileDataService fileDataService, IConnectivity connectivity, IGeolocation geolocation)
    public FilesViewModel(FileDataService fileDataService, IConnectivity connectivity)
    {
        Title = "Far Away Files Access";
        this.fileDataService = fileDataService;
        this.connectivity = connectivity;
        //JEEWEE
        //this.geolocation = geolocation;
    }
    
    [RelayCommand]
    async Task GoToDetails(Model.FileData fileData)
    {
        if (fileData == null)
            return;

        await Shell.Current.GoToAsync(nameof(DetailsPage), true, new Dictionary<string, object>
        {
            {"FileData", fileData }
        });
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
            FilesData.Clear();

            var folderPickerResult = await FolderPicker.PickAsync("");
            if (! folderPickerResult.IsSuccessful)
            {
                throw new Exception($"FolderPicker not successful or cancelled");
            }

            string rootPath = folderPickerResult.Folder?.Path;

            foreach (var fileData in fileDataService.GetFilesData(rootPath))
            {
                FilesData.Add(fileData);
            }
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
    [RelayCommand]
    async Task GetClosestMonkey()
    {
        if (IsBusy || FilesData.Count == 0)
            return;

        try
        {
            //JEEWEE
            //// Get cached location, else get real location.
            //var location = await geolocation.GetLastKnownLocationAsync();
            //if (location == null)
            //{
            //    location = await geolocation.GetLocationAsync(new GeolocationRequest
            //    {
            //        DesiredAccuracy = GeolocationAccuracy.Medium,
            //        Timeout = TimeSpan.FromSeconds(30)
            //    });
            //}

            //// Find closest monkey to us
            //var first = Monkeys.OrderBy(m => location.CalculateDistance(
            //    new Location(m.Latitude, m.Longitude), DistanceUnits.Miles))
            //    .FirstOrDefault();

            //await Shell.Current.DisplayAlert("", first.Name + " " +
            //    first.Location, "OK");

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to query location: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
    }
}
