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
//using Microsoft.Maui.Controls.Compatibility.Platform.Android;
//using Android.Content.Res;
//using Android.Content.Res;

namespace FarFiles.ViewModel;

public partial class MainPageViewModel : BaseViewModel
{
    //JEEWEE
    //protected SettingsService _settingsService;
    //public MainPageViewModel(SettingsService settingsService)

    protected int _numSent = 0;
    protected UdpClient _udpClient = null;
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

    public string _clientMsg = "";
    public string ClientMsg
    {
        get => _clientMsg;
        set
        {
            if (_clientMsg != value)
            {
                _clientMsg = value;
                OnPropertyChanged();
            }
        }
    }

    public string FullPathRoot
    {
        // unlike ConnectKey, FullPathRoot needs an intermediate variable for binding
        // otherwise after picking a folder, OnPropertyChanged does not work for
        // variables in Settings because it's not INotifyPropertyChanged .
        // Although it works at start app. As ChatGPT explained to me (JWdP)
        get => Settings.FullPathRoot;
        set
        {
            if (Settings.FullPathRoot != value)
            {
                Settings.FullPathRoot = value;
                OnPropertyChanged();
            }
        }
    }

    public bool _visClientMsg = false;
    public bool VisClientMsg
    {
        get => _visClientMsg;
        set
        {
            if (_visClientMsg != value)
            {
                _visClientMsg = value;
                OnPropertyChanged();
            }
        }
    }

    //JEEWEE
    //public string FullPathRoot { get; set; } = "";
    //public int Idx0isSvr1isCl { get; set; } = 0;
    //public string ConnectKey { get; set; } = "";

    protected string _lblInfo1 = "";
    public string LblInfo1
    {
        get => _lblInfo1;
        set
        {
            _lblInfo1 = value; OnPropertyChanged();
        }
    }

    protected string _lblInfo2 = "";
    public string LblInfo2
    {
        get => _lblInfo2;
        set
        {
            _lblInfo2 = value; OnPropertyChanged();
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

            FullPathRoot = folderPickerResult.Folder?.Path;
        }
        catch (Exception exc)
        {
            await Shell.Current.DisplayAlert("Error",
                $"Unable to browse for root folder: {exc.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }


    [RelayCommand]
    async Task ConnectAndDoConversation()
    {
        if (IsBusy)
            return;

        if (String.IsNullOrEmpty(Settings.FullPathRoot))
        {
            await Shell.Current.DisplayAlert("Info",
                "Connect: Please browse for root first", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            string msg = "";

            int udpSvrPort = 0;
            if (0 == MauiProgram.Settings.Idx0isSvr1isCl)
            {
                // server: get udp port from Stun server
                var udpIEndPoint = await GetUdpSvrIEndPointFromStun();
                if (null == udpIEndPoint)
                    throw new Exception("Error getting data from Stun Server");
                udpSvrPort = udpIEndPoint.Port;
                if (udpSvrPort <= 0)
                    throw new Exception("Wrong data from Stun Server");
            }

            //JEEWEE
            //using (var client = new HttpClient())
            //{
            //    var url = "https://www.cadh5.com/farfiles/farfiles.php";

            //    //JEEWEE
            //    //var requestData = new { ConnectKey = ConnectKey, SvrCl = Idx0isSvr1isCl, LocalIP = GetLocalIP() };
            //    var requestData = new { ConnectKey = MauiProgram.Settings.ConnectKey, UdpSvrPort = udpSvrPort, LocalIP = GetLocalIP() };
            //    var json = JsonSerializer.Serialize(requestData);
            //    var content = new StringContent(json, Encoding.UTF8, "application/json");

            //    var response = await client.PostAsync(url, content);
            //    response.EnsureSuccessStatusCode();
            //    msg = await response.Content.ReadAsStringAsync();
            //}
            MauiProgram.UdpSvrPort_0isclient = udpSvrPort;
            MauiProgram.StrLocalIP = GetLocalIP();
            msg = await MauiProgram.PostToCentralServerAsync("REGISTER",
                udpSvrPort,        // if 0 then this is client
                MauiProgram.StrLocalIP);

            string errMsg = GetJsonProp(msg, "errMsg");

            if ("" != errMsg)
            {
                throw new Exception(errMsg);
            }

            MauiProgram.Connected = true;
            IsBusy = true;

            if (MauiProgram.Settings.Idx0isSvr1isCl == 0)
            {
                // server: do conversation: in loop: listen for client msgs and response
                LblInfo1 = $"Connected; listening for client contact ...";
                while (true)
                {
                    await ListenMsgAndSendMsgAsSvrAsync(udpSvrPort);
                }
            }
            else if (MauiProgram.Settings.Idx0isSvr1isCl == 1)
            {
                // client: connect to server
                string ipData = GetJsonProp(msg, "ipData");
                if (String.IsNullOrEmpty(ipData))
                {
                    errMsg = "Internal error (ipData 1)";
                }
                else
                {
                    errMsg = ConnectServerFromClient(ipData);
                }
                    
                if ("" != errMsg)
                {
                    throw new Exception(errMsg);
                }

                // client: conversation is done by other buttons and controls
            }

            //JEEWEE: INTERESTING CODE
            //===============================================
            //if (false)
            //{
            //    var stunData = await GetPublicIPAsync();
            //    await Shell.Current.DisplayAlert("GetPublicIPAsync",
            //        $"publicIP={stunData.publicIP}, natType={stunData.natType}", "Cancel");
            //}

            //if (false)
            //{
            //    msg = await TestStunUdpConnection();
            //    await Shell.Current.DisplayAlert("Test", msg, "Cancel");
            //}
            //===============================================
        }
        catch (Exception exc)
        {
            await Shell.Current.DisplayAlert("Error",
                MauiProgram.ExcMsgWithInnerMsgs(exc), "OK");
            IsBusy = false;
        }
    }





    [RelayCommand]
    async Task SendClientMsg()
    {
        //JEEWEE: MAYBE IS SENDCLIENTMSGBUSY ?
        //if (IsBusy)
        //    return;

        //JEEWEE
        //OnPropertyChanged(nameof(ClientMsg));

        try
        {
            //JEEWEE
            //IsBusy = true;

            if (1 == MauiProgram.Settings.Idx0isSvr1isCl)        // client
            {
                LblInfo2 = "";
                byte[] sendMsg = Encoding.UTF8.GetBytes(ClientMsg);
                //JEEWEE seems 20 is minimum?
                //await udpClient.SendAsync(new byte[20], 20);
                LblInfo1 = $"sending to server ...";
                int iResult = await _udpClient.SendAsync(sendMsg, sendMsg.Length);

                LblInfo1 = $"sent to server: '{ClientMsg}'";
                LblInfo2 = $"sent bytes: {iResult}; waiting for server ...";
                var response = await _udpClient.ReceiveAsync(
                                        new CancellationTokenSource(5000).Token);

                LblInfo2 = $"Received from server: '{Encoding.UTF8.GetString(response.Buffer)}'";
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlert("Error",
                $"Response from server timed out", "OK");
        }
        catch (Exception exc)
        {
            await Shell.Current.DisplayAlert("Error",
                $"Unable to receive from server: {MauiProgram.ExcMsgWithInnerMsgs(exc)}", "OK");
        }
        //JEEWEE
        //finally
        //{
        //    IsBusy = false;
        //}
    }

    //JEEWEE
    //protected void LoadSettings()
    //{
    //    Settings = _settingsService.LoadFromFile();
    //}

    protected async Task ListenMsgAndSendMsgAsSvrAsync(int udpSvrPort)
    {
        using (var udpServer = new UdpClient(udpSvrPort))
        {
            UdpReceiveResult received = await udpServer.ReceiveAsync();
            LblInfo1 = $"Received from client: '{Encoding.UTF8.GetString(received.Buffer)}'";

            //5️ Respond to client(hole punching)
            string strResp = $"Hello {++_numSent} from server!";
            byte[] response = Encoding.UTF8.GetBytes(strResp);
            LblInfo2 = $"sending '{strResp}' ...";
            await udpServer.SendAsync(response, response.Length, received.RemoteEndPoint);
            LblInfo2 = $"sent: '{strResp}'";
        }
    }


    protected string ConnectServerFromClient(string ipData)
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
                _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0)); // Let the OS pick the local address
                _udpClient.Connect(new IPEndPoint(
                        new IPAddress(ipAddress.Split('.')
                            .Select(p => Convert.ToByte(p))
                            .ToArray()),
                        Convert.ToInt32(publicUdpPortSvr)));
                LblInfo2 = "Connected to server.";
                VisClientMsg = true;
                return "";
            }
            catch (Exception exc)
            {
                _udpClient = null;
                if (1 == i)
                {
                    return MauiProgram.ExcMsgWithInnerMsgs(exc);
                }
            }
        }

        return "PROGRAMMERS: ConnectServerFromClient: impossible";
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
        catch (SocketException exc)
        {
            return $"TestStunUdpConnection(): error: {MauiProgram.ExcMsgWithInnerMsgs(exc)}";
        }
    }


    protected static async Task<IPEndPoint> GetUdpSvrIEndPointFromStun()
    {
        //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! MAKE THIS A SETTING, SOMEHOW
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
