using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;

using STUN.Client;
using STUN.Enums;

using FarFiles.Services;
//JEEWEE
//using Java.Util;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using System.Threading.Channels;
//JEEWEE
//using Android.Content.Res;

namespace FarFiles.ViewModel;

public partial class MainPageViewModel : BaseViewModel
{
    //JEEWEE
    //protected SettingsService _settingsService;
    //public MainPageViewModel(SettingsService settingsService)
    public MainPageViewModel()
    {
        Title = "Far Away Files Access";

        //JEEWEE
        //_settingsService = settingsService;
        //LoadSettings();

        //JEEWEE: seems xaml cannot bind to MauiProgram.Settings directly
        Settings = MauiProgram.Settings;
    }

    public Settings Settings { get; set; }

    //JEEWEE
    //public string FullPathRoot { get; set; } = "";
    //public int Idx0isSvr1isCl { get; set; } = 0;
    //public string ConnectKey { get; set; } = "";

    protected string _lblInfo = "";
    public string LblInfo
    {
        get => _lblInfo;
        set
        {
            _lblInfo = value; OnPropertyChanged();
        }
    }

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

            MauiProgram.Settings.FullPathRoot = folderPickerResult.Folder?.Path;
            OnPropertyChanged(nameof(Settings.FullPathRoot));
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

            string msg = "";

            if (true)
            {
                int udpSvrPort = 0;
                if (0 == MauiProgram.Settings.Idx0isSvr1isCl)        // server
                {
                    var udpIEndPoint = await GetUdpSvrIEndPointFromStun();
                    if (null == udpIEndPoint)
                        throw new Exception("Error getting data from Stun Server");
                    udpSvrPort = udpIEndPoint.Port;
                }

                using (var client = new HttpClient())
                {
                    var url = "https://www.cadh5.com/farfiles/farfiles.php";

                    //JEEWEE
                    //var requestData = new { ConnectKey = ConnectKey, SvrCl = Idx0isSvr1isCl, LocalIP = GetLocalIP() };
                    var requestData = new { ConnectKey = MauiProgram.Settings.ConnectKey, UdpSvrPort = udpSvrPort, LocalIP = GetLocalIP() };
                    var json = JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();
                    msg = await response.Content.ReadAsStringAsync();
                }

                await Shell.Current.DisplayAlert("Info", msg, "Cancel");

                if (MauiProgram.Settings.Idx0isSvr1isCl == 0)
                {
                    msg = await ListenAsSvr(udpSvrPort);
                    await Shell.Current.DisplayAlert("ListenAsSvr", msg, "Cancel");
                }
                else if (MauiProgram.Settings.Idx0isSvr1isCl == 1)
                {
                    string ipData = GetJsonProp(msg, "ipData");
                    if (String.IsNullOrEmpty(ipData))
                    {
                        msg = "Internal error (ipData 1)";
                    }
                    else
                    {
                        msg = await TestConnectionFromClient(ipData);
                        await Shell.Current.DisplayAlert("Test", msg, "Cancel");
                    }
                }
            }

            if (false)
            {
                var stunData = await GetPublicIPAsync();
                await Shell.Current.DisplayAlert("GetPublicIPAsync",
                    $"publicIP={stunData.publicIP}, natType={stunData.natType}", "Cancel");
            }

            if (false)
            {
                msg = await TestStunUdpConnection();
                await Shell.Current.DisplayAlert("Test", msg, "Cancel");
            }
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

    //JEEWEE
    //protected void LoadSettings()
    //{
    //    Settings = _settingsService.LoadFromFile();
    //}

    protected async Task<string> ListenAsSvr(int udpSvrPort)
    {
        LblInfo = $"listening on port {udpSvrPort} ...";
        using (var udpServer = new UdpClient(udpSvrPort))
        {
            UdpReceiveResult received = await udpServer.ReceiveAsync();
            string msg = $"Received from {received.RemoteEndPoint}: {Encoding.UTF8.GetString(received.Buffer)}";

            //5️⃣ Respond to client(hole punching)
            byte[] response = Encoding.UTF8.GetBytes("Hello from server!");
            LblInfo = $"sending to {received.RemoteEndPoint} ...";
            await udpServer.SendAsync(response, response.Length, received.RemoteEndPoint);
            LblInfo = $"";
            return msg;
        }
    }


    protected async Task<string> TestConnectionFromClient(string ipData)
    {
        string[] parts = ipData.Split(',');
        if (parts.Length < 3)
            return "Internal error (ipData 2)";

        string publicIpSvr = parts[0];
        string publicUdpPortSvr = parts[1];
        string localIpSvr = parts[2];

        for (int i = 0; i < 2; i++)
        {
            try
            {
                string ipAddress = 0 == i ? localIpSvr : publicIpSvr;
                using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0)); // Let the OS pick the local address
                udpClient.Connect(new IPEndPoint(
                        new IPAddress(ipAddress.Split('.')
                            .Select(p => Convert.ToByte(p))
                            .ToArray()),
                        Convert.ToInt32(publicUdpPortSvr)));
                byte[] sendMsg = Encoding.UTF8.GetBytes("Client test string90");
                //JEEWEE seems 20 is minimum?
                //await udpClient.SendAsync(new byte[20], 20);
                LblInfo = $"sending to {ipAddress}:{publicUdpPortSvr} ...";
                int iResult = await udpClient.SendAsync(sendMsg, sendMsg.Length);

                LblInfo = $"iResult={iResult}; waiting for {ipAddress}:{publicUdpPortSvr} ...";
                var response = await udpClient.ReceiveAsync(
                    new CancellationTokenSource(5000).Token);

                return $"TestConnectionFromClient(): Received {response.Buffer.Length} bytes from server {response.RemoteEndPoint}";
            }
            catch (Exception ex)
            {
                if (1 == i)
                {
                    return ex.Message;
                }
            }
        }

        return "PROGRAMMERS: TestConnectionFromClient: impossible";
    }



    protected async Task<string> TestStunUdpConnection()
    {
        //JEEWEE
        //var stunServer = "stun.l.google.com";
        //var stunPort = 19302;
        var stunServer = "stun.sipgate.net";
        var stunPort = 3478;

        using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        udpClient.Connect(stunServer, stunPort);

        var message = new byte[20]; // Minimal STUN request (not a full valid STUN packet)
        await udpClient.SendAsync(message, message.Length);

        try
        {
            var response = await udpClient.ReceiveAsync();
            return $"TestStunUdpConnection(): received {response.Buffer.Length} bytes from {response.RemoteEndPoint}";
        }
        catch (SocketException ex)
        {
            return $"TestStunUdpConnection(): error: {ex.Message}";
        }
    }


    protected static async Task<IPEndPoint> GetUdpSvrIEndPointFromStun()
    {
        //JEEWEE
        //var stunServer = "stun.l.google.com";
        //var stunPort = 19302;
        var stunServer = "stun.sipgate.net";
        var stunPort = 3478;

        IPEndPoint localUdpEndPoint;
        using (var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0)))
        {
            localUdpEndPoint = ((IPEndPoint)udpClient.Client.LocalEndPoint);
            int localUdpPort = localUdpEndPoint.Port;
        }

        // 2️⃣ Use STUN to find public IP & port
        // Resolve STUN server hostname to an IP address
        var addresses = await Dns.GetHostAddressesAsync(stunServer);
        var stunServerIP = addresses
            .First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        var stunServerEndPoint = new IPEndPoint(stunServerIP, stunPort);

        // Create StunClient3489 instance
        var stunClient = new StunClient3489(stunServerEndPoint, localUdpEndPoint);

        await stunClient.QueryAsync(); // Perform the STUN request

        return stunClient.State.PublicEndPoint;

        //JEEWEE
        //if (stunClient.State.PublicEndPoint != null)
        //{
        //    string publicIp = stunClient.State.PublicEndPoint.Address.ToString();
        //    int publicPort = stunClient.State.PublicEndPoint.Port;
        //    Console.WriteLine($"Detected Public IP: {publicIp}, Port: {publicPort}");
        //}
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


    public async Task<(IPAddress publicIP, string natType)> GetPublicIPAsync()
    {
        //JEEWEE
        //var stunServer = "stun.l.google.com";
        //var stunServer = "stun1.l.google.com";
        //var stunPort = 19302;

        //stun.l.google.com   19302
        //stun1.l.google.com  19302
        //stun.sipgate.net    3478
        //stun.voip.blackberry.com    3478

        var stunServer = "stun.l.google.com";
        var stunPort = 19302;


        using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        udpClient.Connect(stunServer, stunPort);

        var localEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint!;

        // Resolve STUN server hostname to an IP address
        var addresses = await Dns.GetHostAddressesAsync(stunServer);
        var stunServerIP = addresses
            .First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        var stunServerEndPoint = new IPEndPoint(stunServerIP, stunPort);

        // Create StunClient3489 instance
        var stunClient = new StunClient3489(localEndPoint, stunServerEndPoint);
        //var stunClient = new StunClient5389UDP(localEndPoint, stunServerEndPoint);

        await stunClient.QueryAsync(); // Perform the STUN request

        if (stunClient.State.PublicEndPoint == null)
        {
            throw new Exception("Failed to determine public IP");
        }

        //StunClient3489:
        return (stunClient.State.PublicEndPoint.Address, stunClient.State.NatType.ToString());

        //StunClient5389UDP:
        //return (stunClient.State.PublicEndPoint.Address, stunClient.State.MappingBehavior.ToString());
    }


    protected static string GetJsonProp(string strJson, string propName)
    {
        string search = $"\"{propName}\":\"";
        int idx = strJson.IndexOf(search);
        if (idx == -1)
            return "";
        idx += search.Length;
        int idx2 = strJson.IndexOf("\"", idx);
        if (idx2 == -1)
            return "";

        return strJson.Substring(idx, idx2 - idx);
    }


}
