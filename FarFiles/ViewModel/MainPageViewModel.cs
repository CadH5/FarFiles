using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;

using FarFiles.Services;
//using Java.Util;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
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
            //JEEWEE
            //using (HttpClient httpClient = new())
            //{
            //// JWdP 20240403
            //// Not on localhost, but on real site, I got contents of a different html doc
            //// for robots!
            //// Suggestion CoPilot: mimic a httpClient browser. It works, thanks!
            //httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

            //// Open a stream to read the content
            //Stream stream = await httpClient.GetStreamAsync("https://www.cadh5.com/MiraClientes/zafftr.php");

            //string phpContent = "";
            //// Read the content as text
            //using (StreamReader reader = new StreamReader(stream))
            //{
            //    phpContent = reader.ReadToEnd();
            //}

            //await Shell.Current.DisplayAlert("Contents", phpContent, "Cancel");

            string msg = "";
            using (var client = new HttpClient())
            {
                var url = "https://www.cadh5.com/farfiles/farfiles.php";

                var requestData = new { ConnectKey = ConnectKey, SvrCl = Idx0isSvr1isCl, LocalIP = GetLocalIP() };
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                msg = await response.Content.ReadAsStringAsync();
            }

            await Shell.Current.DisplayAlert("Info", msg, "Cancel");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error",
                $"Unable to connect: {ex.Message}", "Cancel");
        }
        finally
        {
            IsBusy = false;
        }
    }


    protected static string GetLocalIP()
    {
        // 20250309 ChatGPT composed me this:
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up) // Only active network interfaces
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
            .Select(a => a.Address.ToString())
            .FirstOrDefault() ?? "";
    }
}
