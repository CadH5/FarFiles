using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;

using FarFiles.Services;
using System.Threading;

namespace FarFiles.ViewModel;

public partial class MainPageViewModel : BaseViewModel
{
    public MainPageViewModel()
    {
        Title = "Far Away Files Access";
    }

    public string FullPathRoot { get; set; }
    public int Idx0isSvr1isCl { get; set; }
    public string ConnectKey { get; set; }

    [RelayCommand]
    async Task Browse()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var folderPickerResult = await FolderPicker.PickAsync("");
            if (!folderPickerResult.IsSuccessful)
            {
                throw new Exception($"FolderPicker not successful or cancelled");
            }

            FullPathRoot = folderPickerResult.Folder?.Path;
            OnPropertyChanged(nameof(FullPathRoot));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error",
                $"Unable to browse for root folder: {ex.Message}", "OK", null);
        }
        finally
        {
            IsBusy = false;
        }
    }


    [RelayCommand]
    async Task Connect()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            await Shell.Current.DisplaySnackbar(
                $"Idx0isSvr1isCl={Idx0isSvr1isCl}, ConnectKey='{ConnectKey}'");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error",
                $"Unable to connect: {ex.Message}", "OK", null);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
