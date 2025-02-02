namespace FarFiles.ViewModel;

[QueryProperty(nameof(FileData), "FileData")]
public partial class FileDetailsViewModel : BaseViewModel
{
    //JEEWEE
    //IMap map;
    //public FileDetailsViewModel(IMap map)
    //{
    //    this.map = map;
    //}
    public FileDetailsViewModel()
    {
    }

    [ObservableProperty]
    Model.FileData fileData;

    [RelayCommand]
    async Task OpenMap()
    {
        try
        {
            //JEEWEE
            //await map.OpenAsync(Monkey.Latitude, Monkey.Longitude, new MapLaunchOptions
            //{
            //    Name = Monkey.Name,
            //    NavigationMode = NavigationMode.None
            //});
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to launch maps: {ex.Message}");
            await Shell.Current.DisplayAlert("Error, no Maps app!", ex.Message, "OK");
        }
    }
}
