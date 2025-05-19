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
//using FarFiles.Platforms.Android;
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
    protected FileDataService _fileDataService;
    protected CopyMgr _copyMgr = null;

    public MainPageViewModel(FileDataService fileDataService)
    {
        Title = "Far Away Files Access";
        _fileDataService = fileDataService;
        MauiProgram.Info.MainPageVwModel = this;
        MauiProgram.Log.LogLine($"FarFiles: started");

        //JEEWEE
        //_settingsService = settingsService;
        //LoadSettings();
    }

    // xaml cannot bind to MauiProgram.Settings directly.
    // And for FullPathRoot an extra measure is necessary
    public Settings Settings { get; protected set; } = MauiProgram.Settings;

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

    
    public bool _isBtnConnectVisible = true;
    public bool IsBtnConnectVisible
    {
        get => _isBtnConnectVisible;
        set
        {
            if (_isBtnConnectVisible != value)
            {
                _isBtnConnectVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBtnBackToFilesVisible));
            }
        }
    }

    public bool IsBtnBackToFilesVisible
    {
        get => ! IsBtnConnectVisible;
    }

    public string FullPathRoot
    {
        // unlike ConnectKey, FullPathRoot needs an intermediate variable for binding
        // otherwise after picking a folder, OnPropertyChanged does not work for
        // variables in Settings because it's not INotifyPropertyChanged .
        // Although it works at start app. As ChatGPT explained to me (JWdP)
        get => MauiProgram.Settings.FullPathRoot;
#if ANDROID
#else
        set
        {
            if (MauiProgram.Settings.FullPathRoot != value)
            {
                MauiProgram.Settings.FullPathRoot = value;
                OnPropertyChanged();
            }
        }
#endif
    }

    //JEEWEE
    //public object AndroidUri { get; set; } = null;      // Android.Net.Uri

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


    /// <summary>
    /// Log if secret log option is on (see Log class)
    /// </summary>
    /// <param name="str"></param>
    protected void Log(string str)
    {
        MauiProgram.Log.LogLine(str);
    }

    [RelayCommand]
    async Task Browse()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

#if ANDROID
            //JEEWEE
            //AndroidUri = Android.Net.Uri.Parse(folderPickerResult.Folder.Path);
            //AndroidUri = (Android.Net.Uri)(Java.Lang.Object)folderPickerResult.Folder.Uri;
            //AndroidUri = folderPickerResult.Folder.Uri;

            //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!! PUT COMMENT (why the heck is this necessary)
            MauiProgram.SaveSettings();
            Settings.AndroidUriRoot = await MauiProgram.AndroidFolderPicker.PickFolderAsync();
            var context = global::Android.App.Application.Context;

            // JWdP 20250518 Another necessary ^&*%^%^, provided to me by ChatGPT,
            // else the deserialized AndroidUri from settings results in Invalid Uri, next time.
            context.ContentResolver.TakePersistableUriPermission(
                Settings.AndroidUriRoot,
                Android.Content.ActivityFlags.GrantReadUriPermission |
                Android.Content.ActivityFlags.GrantWriteUriPermission);

            MauiProgram.SaveSettings();
            OnPropertyChanged("FullPathRoot");

#else
            var folderPickerResult = await FolderPicker.PickAsync("");
            if (!folderPickerResult.IsSuccessful)
            {
                throw new Exception($"FolderPicker not successful or cancelled");
            }

            FullPathRoot = folderPickerResult.Folder?.Path;     // this calls OnPropertyChanged
#endif

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



    protected async void OpenClientJEEWEE()
    {
        string[] folderNames = { "aa", "folderB xxxxxxxxxx yyyyyyyyyyyy zzzzzz" };
        string[] fileNames = { "bb", "fileC",
            "file 64567 56756756 56588568 4685588 54788",
            "file2 64567 56756756 56588568 4685588 54788",
            "file3 64567 56756756 56588568 4685588 54788",
            "file4 64567 56756756 56588568 4685588 54788",
            "file5 64567 56756756 56588568 4685588 54788",
            "file6 64567 56756756 56588568 4685588 54788",
            "file7 64567 56756756 56588568 4685588 54788",
            "file8 64567 56756756 56588568 4685588 54788",
            "file9 64567 56756756 56588568 4685588 54788",
            "file0 64567 56756756 56588568 4685588 54788",
            "file1 64567 56756756 56588568 4685588 54788",
            "file2 64567 56756756 56588568 4685588 54788",
            "file3 64567 56756756 56588568 4685588 54788",
            "file4 64567 56756756 56588568 4685588 54788",
            "file5 64567 56756756 56588568 4685588 54788",
            "file6 64567 56756756 56588568 4685588 54788",
            "file7 64567 56756756 56588568 4685588 54788",
            "file8 64567 56756756 56588568 4685588 54788",
            "file9 64567 56756756 56588568 4685588 54788",
            "file30 64567 56756756 56588568 4685588 54788",
            "file31 64567 56756756 56588568 4685588 54788",
            "file32 64567 56756756 56588568 4685588 54788",
            "file33 64567 56756756 56588568 4685588 54788",
            "file34 64567 56756756 56588568 4685588 54788",
            "file35 64567 56756756 56588568 4685588 54788",
            "file36 64567 56756756 56588568 4685588 54788",
            "file37 64567 56756756 56588568 4685588 54788",
            "file38 64567 56756756 56588568 4685588 54788",
            "file39 64567 56756756 56588568 4685588 54788",
            "file40 64567 56756756 56588568 4685588 54788",
            "file41 64567 56756756 56588568 4685588 54788",
        };
        MauiProgram.Info.CurrSvrFolders = folderNames.Order().Select(
            f => new FileOrFolderData(f, true, false)).ToArray();
        MauiProgram.Info.CurrSvrFiles = fileNames.Order().Select(
            f => new FileOrFolderData(f, false, false)).ToArray();
        await Shell.Current.GoToAsync(nameof(ClientPage), true);
    }



    public void OnCloseThings()
    {
        _copyMgr?.Dispose();
    }


    [RelayCommand]
    async Task ConnectAndDoConversation()
    {
        if (IsBusy)
            return;

        //JWdP 20250507 Introduced "unittests", to be executed from this button if incommented
        //====================================================================================
        //await MauiProgram.Tests.DoTestsAsync(_fileDataService);
        //return;
        //====================================================================================

        //OpenClientJEEWEE();

        if (String.IsNullOrEmpty(MauiProgram.Settings.FullPathRoot))
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
                var udpIEndPoint = await GetUdpSvrIEndPointFromStun(Settings);
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
            MauiProgram.Info.UdpSvrPort = udpSvrPort;
            MauiProgram.StrLocalIP = GetLocalIP();
            msg = await MauiProgram.PostToCentralServerAsync("REGISTER",
                udpSvrPort,        // if 0 then this is client
                MauiProgram.StrLocalIP);

            string errMsg = GetJsonProp(msg, "errMsg");
            if (String.IsNullOrEmpty(errMsg))
                errMsg = IpDataToInfo(GetJsonProp(msg, "ipData"));

            if ("" != errMsg)
            {
                throw new Exception(errMsg);
            }

            MauiProgram.Info.Connected = true;
            IsBusy = true;

            if (MauiProgram.Settings.Idx0isSvr1isCl == 0)
            {
                // server: do conversation: in loop: listen for client msgs and response
                LblInfo1 = $"Connected; listening for client contact ...";
                using (var udpServer = new UdpClient(udpSvrPort))
                {
                    while (true)
                    {
                        await ListenMsgAndSendMsgAsSvrAsync(udpServer);
                    }
                }
            }
            else if (MauiProgram.Settings.Idx0isSvr1isCl == 1)
            {
                // client: connect to server
                errMsg = ConnectServerFromClient();
                if ("" != errMsg)
                {
                    throw new Exception(errMsg);
                }

                // client: send request info rootpath and recieve answer
                await SndFromClientRecievePathInfo_msgbxs_Async();

                await Shell.Current.GoToAsync(nameof(ClientPage), true);
                IsBtnConnectVisible = false;
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


	/// <summary>
	/// Send MauiProgram.Info.SvrPathParts to server, receive folders and files,
	/// set those in MauiProgram.Info.CurrSvrFolders, MauiProgram.Info.CurrSvrFiles
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public async Task SndFromClientRecievePathInfo_msgbxs_Async()
    {
        var msgSvrCl = new MsgSvrClPathInfoRequest(MauiProgram.Info.SvrPathParts);
        byte[] byRecieved = await SndFromClientRecieve_msgbxs_Async(
                            msgSvrCl.Bytes);
        if (byRecieved.Length == 0)
            return;

        MsgSvrClBase msgSvrClAnswer = MsgSvrClBase.CreateFromBytes(byRecieved);
        msgSvrClAnswer.CheckExpectedTypeMaybeThrow(typeof(MsgSvrClPathInfoAnswer));
        Log($"client: received bytes: {byRecieved.Length}, MsgSvrClPathInfoAnswer");

        ((MsgSvrClPathInfoAnswer)msgSvrClAnswer).GetFolderAndFileNamesAndSizes(
                out string[] folderNames, out string[] fileNames, out long[] fileSizes);

        MauiProgram.Info.CurrSvrFolders = folderNames.Order().Select(
            f => new FileOrFolderData(f, true, false)).ToArray();
        int i = 0;
        MauiProgram.Info.CurrSvrFiles = fileNames.Select(
            f => new FileOrFolderData(f, false, false, fileSizes[i++]))
            .OrderBy(f => f.Name)
            .ToArray();
    }


    public async Task CopyFromSvr_msgbxs_Async(FileOrFolderData[] selecteds)
    {
        MsgSvrClBase msgSvrCl = new MsgSvrClCopyRequest(MauiProgram.Info.SvrPathParts,
                selecteds.Where(f => f.IsDir).Select(f => f.Name),
                selecteds.Where(f => ! f.IsDir).Select(f => f.Name));

        using (var copyMgr = new CopyMgr(_fileDataService))
        {
            while (true)
            {
                byte[] byRecieved = await SndFromClientRecieve_msgbxs_Async(
                                    msgSvrCl.Bytes);
                if (byRecieved.Length == 0)
                    return;

                MsgSvrClBase msgSvrClAnswer = MsgSvrClBase.CreateFromBytes(byRecieved);
                msgSvrClAnswer.CheckExpectedTypeMaybeThrow(typeof(MsgSvrClCopyAnswer));
                Log($"client: received bytes: {byRecieved.Length}, MsgSvrClCopyAnswer");

                if (copyMgr.CreateOnClientFromNextPart((MsgSvrClCopyAnswer)msgSvrClAnswer))
                    break;          // ready

                msgSvrCl = new MsgSvrClCopyNextpartRequest();

                //JEEWEE
                //if (!(msgSvrClAnswer is MsgSvrClCopyAnswer))
                //    throw new Exception(
                //        $"Expected from server: MsgSvrClCopyAnswer, got: {msgSvrClAnswer.GetType()}");

            }

            string nl = Environment.NewLine;
            await Shell.Current.DisplayAlert("Copied",
                $"Folders created: {copyMgr.NumFoldersCreated}{nl}" +
                $"Files created: {copyMgr.NumFilesCreated}{nl}" +
                $"Files overwritten: {copyMgr.NumFilesOverwritten}{nl}" +
                $"Files skipped: {copyMgr.NumFilesSkipped}{nl}" +
                (copyMgr.NumErrs > 0 ? $"ERRORS: {copyMgr.NumErrs}{nl}" : ""),
                "OK");
        }
    }




    [RelayCommand]
    async Task BackToFiles()
    {
        await Shell.Current.GoToAsync(nameof(ClientPage), true);
    }



    [RelayCommand]
    async Task OpenAdvancedDlg()
    {
        //JEEWEE
        await Shell.Current.GoToAsync(nameof(AdvancedPage), true);
    }




    [RelayCommand]
    async Task SendClientMsg()
    {
        //JEEWEE: MAYBE IS SENDCLIENTMSGBUSY ?
        //if (IsBusy)
        //    return;

        //JEEWEE
        //OnPropertyChanged(nameof(ClientMsg));

        //JEEWEE
        //IsBusy = true;

        if (1 == MauiProgram.Settings.Idx0isSvr1isCl)        // client
        {
            //JEEWEE
            //LblInfo2 = "";
            //byte[] sendMsg = Encoding.UTF8.GetBytes(ClientMsg);
            ////JEEWEE seems 20 is minimum?
            ////await udpClient.SendAsync(new byte[20], 20);
            //LblInfo1 = $"sending to server ...";
            //int iResult = await _udpClient.SendAsync(sendMsg, sendMsg.Length);

            //LblInfo1 = $"sent to server: '{ClientMsg}'";
            //LblInfo2 = $"sent bytes: {iResult}; waiting for server ...";
            //var response = await _udpClient.ReceiveAsync(
            //                        new CancellationTokenSource(5000).Token);

            //LblInfo2 = $"Received from server: '{Encoding.UTF8.GetString(response.Buffer)}'";

            byte[] recBytes = await SndFromClientRecieve_msgbxs_Async(
                            Encoding.UTF8.GetBytes(ClientMsg));
            Log($"client: received bytes: {recBytes.Length}");
            LblInfo2 = $"Received from server: '{Encoding.UTF8.GetString(recBytes)}'";
        }
    }

    //JEEWEE
    //protected void LoadSettings()
    //{
    //    Settings = _settingsService.LoadFromFile();
    //}

    /// <summary>
    /// Sends bytes to server and receives bytes. If exception, displays alert and returns [0] bytes
    /// </summary>
    /// <param name="sendBytes"></param>
    /// <returns></returns>
    protected async Task<byte[]> SndFromClientRecieve_msgbxs_Async(byte[] sendBytes)
    {
        int iTry = 0;
        int maxTries = 1;       //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! 3 GIVES TROUBLE
        while (iTry < maxTries)
        {
            try
            {
                LblInfo2 = "";
                LblInfo1 = $"sending to server ...";
                int iResult = await _udpClient.SendAsync(sendBytes, sendBytes.Length);

                LblInfo1 = $"intended for server: {sendBytes.Length} bytes";
                LblInfo2 = $"bytes really sent: {iResult}; waiting for server ...";
                Log("client: " + LblInfo2);

                UdpReceiveResult response = await _udpClient.ReceiveAsync(
                                    new CancellationTokenSource(40000).Token);

                //JEEWEE: SECONDS MAYBE SETTING
                //JEEWEE: ALSO DIR MUST BECOME SPLITTED
                //JEEWEE
                //LblInfo2 = $"Received from server: {Encoding.UTF8.GetString(response.Buffer)}'";
                LblInfo2 = $"Received from server: {response.Buffer.Length} bytes";
                return response.Buffer;
            }
            catch (OperationCanceledException)
            {
                string errMsg = $"Response from server timed out";
                Log($"Try {iTry+1} of {maxTries}: {errMsg}");
                if (iTry == maxTries - 1)
                    await Shell.Current.DisplayAlert("Error", errMsg, "OK");
            }
            catch (Exception exc)
            {
                Log("client: exception; LblInfo1=" + LblInfo1);
                Log("client: exception; LblInfo2=" + LblInfo2);
                string errMsg = $"Unable to receive from server: {MauiProgram.ExcMsgWithInnerMsgs(exc)}";
                Log($"Try {iTry + 1} of {maxTries}: {errMsg}");
                if (iTry == maxTries - 1)
                    await Shell.Current.DisplayAlert("Error", errMsg, "OK");
            }

            iTry++;
            Log($"client: iTry={iTry}");
        }

        //JEEWEE
        //finally
        //{
        //    IsBusy = false;
        //}

        return new byte[0];
    }



    protected async Task ListenMsgAndSendMsgAsSvrAsync(UdpClient udpServer)
    {
        try
        {
            UdpReceiveResult received = await udpServer.ReceiveAsync();
            MsgSvrClBase msgSvrCl = MsgSvrClBase.CreateFromBytes(received.Buffer);
            Log($"server: received bytes: {received.Buffer.Length}, type: {msgSvrCl.Type}");

            MsgSvrClBase msgSvrClAns = null;

            string receivedTxt = "";
            string errToSendTxt = "";
            if (msgSvrCl is MsgSvrClStringSend)
            {
                receivedTxt = ((MsgSvrClStringSend)msgSvrCl).GetString();
                msgSvrClAns = new MsgSvrClStringAnswer();
            }
            else if (msgSvrCl is MsgSvrClPathInfoRequest)
            {
                receivedTxt = msgSvrCl.Type.ToString();

#if ANDROID
                FileOrFolderData[] data = _fileDataService.GetFilesAndFoldersDataAndroid(
                            Settings.AndroidUriRoot,
                            ((MsgSvrClPathInfoRequest)msgSvrCl).GetSvrSubParts());
#else
                string path = Settings.PathFromRootAndSubParts(
                            ((MsgSvrClPathInfoRequest)msgSvrCl).GetSvrSubParts());
                FileOrFolderData[] data = _fileDataService.GetFilesAndFoldersDataWindows(
                            path, SearchOption.TopDirectoryOnly);
#endif
                var dataWithExc = data.Where(d => null != d.ExcThrown).FirstOrDefault();
                if (dataWithExc != null)
                {
                    msgSvrClAns = new MsgSvrClErrorAnswer(dataWithExc.ExcThrown.Message);
                }
                else
                {
                    string[] folderNames = data.Where(d => d.IsDir).Select(d => d.Name).ToArray();
                    string[] fileNames = data.Where(d => !d.IsDir).Select(d => d.Name).ToArray();
                    long[] fileSizes = data.Where(d => !d.IsDir).Select(d => d.FileSize).ToArray();
                    msgSvrClAns = new MsgSvrClPathInfoAnswer(folderNames, fileNames, fileSizes);
                }
            }
            else if (msgSvrCl is MsgSvrClCopyRequest)
            {
                _copyMgr?.Dispose();
                _copyMgr = new CopyMgr(_fileDataService);
                _copyMgr.StartCopyFromSvr((MsgSvrClCopyRequest)msgSvrCl);
                msgSvrClAns = _copyMgr.GetNextPartCopyansFromSvr();
            }
            else if (msgSvrCl is MsgSvrClCopyNextpartRequest)
            {
                if (null == _copyMgr)
                {
                    errToSendTxt =
                        $"Server: wrong request last {msgSvrCl.GetType()}, no active copy process";
                    msgSvrClAns = new MsgSvrClErrorAnswer(errToSendTxt);
                }
                else
                {
                    msgSvrClAns = _copyMgr.GetNextPartCopyansFromSvr();
                    if (msgSvrClAns is MsgSvrClCopyAnswer &&
                        ((MsgSvrClCopyAnswer)msgSvrClAns).IsLastPart)
                    {
                        _copyMgr.Dispose();
                        _copyMgr = null;
                    }
                }
            }
            else
            {
                errToSendTxt =
                    $"Server: received unexpected message type {msgSvrCl.GetType()}";
                msgSvrClAns = new MsgSvrClErrorAnswer(errToSendTxt);
            }

            LblInfo1 = $"Received from client: '{receivedTxt}'";

            //5️ Respond to client(hole punching)
            int numAnswer = MauiProgram.Info.NumAnswersSent + 1;
            LblInfo2 = $"sending answer {numAnswer} ...";
            Log($"server: going to send bytes: {msgSvrClAns.Bytes.Length}, {msgSvrClAns.GetType()}");
            await udpServer.SendAsync(msgSvrClAns.Bytes, msgSvrClAns.Bytes.Length,
                        received.RemoteEndPoint);
            LblInfo2 = $"answer {numAnswer} sent: " +
                (!String.IsNullOrEmpty(errToSendTxt) ? errToSendTxt :
                $"{msgSvrClAns.Bytes.Length} bytes");
            MauiProgram.Info.NumAnswersSent = numAnswer;
        }
        catch (Exception exc)
        {
            Log($"server: EXCEPTION: ListenMsgAndSendMsgAsSvrAsync: " +
                    MauiProgram.ExcMsgWithInnerMsgs(exc));
            await Shell.Current.DisplayAlert("Server: error receiving message or sending answer",
                        MauiProgram.ExcMsgWithInnerMsgs(exc), "OK");
        }
    }



    /// <summary>
    /// Sets received ipData in Info; returns "" or errMsg
    /// </summary>
    /// <param name="ipData"></param>
    /// <returns></returns>
    protected string IpDataToInfo(string ipData)
    {
        string[] parts = ipData.Split(',');
        if (parts.Length < 3)
            return "Internal error (ipData 2)";

        MauiProgram.Info.PublicIpSvrRegistered = parts[0];
        MauiProgram.Info.PublicUdpPortSvrRegistered = parts[1];
        MauiProgram.Info.LocalIpSvrRegistered = parts[2];
        return "";
    }


    protected string ConnectServerFromClient()
    {
        for (int i = 0; i < 2; i++)
        {
            try
            {
                string ipAddress = 0 == i ?
                        MauiProgram.Info.LocalIpSvrRegistered :
                        MauiProgram.Info.PublicIpSvrRegistered;
                _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0)); // Let the OS pick the local address
                _udpClient.Connect(new IPEndPoint(
                        new IPAddress(ipAddress.Split('.')
                            .Select(p => Convert.ToByte(p))
                            .ToArray()),
                        Convert.ToInt32(
                            MauiProgram.Info.PublicUdpPortSvrRegistered)));
                LblInfo2 = "Connected to server.";
                MauiProgram.Info.IpSvrThatClientConnectedTo = ipAddress;
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


    protected static async Task<IPEndPoint> GetUdpSvrIEndPointFromStun(
            Settings settings)
    {
        //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! MAKE THIS A SETTING, SOMEHOW
        //var stunServer = "stun.l.google.com";
        //var stunPort = 19302;
        //var stunServer = "stun.sipgate.net";
        //var stunPort = 3478;

        IPEndPoint localUdpEndPoint;
        using (var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0)))
        {
            localUdpEndPoint = ((IPEndPoint)udpClient.Client.LocalEndPoint);
            int localUdpPort = localUdpEndPoint.Port;
        }

        // 2️⃣ Use STUN to find public IP & port
        // Resolve STUN server hostname to an IP address
        var addresses = await Dns.GetHostAddressesAsync(settings.StunServer);
        var stunServerIP = addresses
            .First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        var stunServerEndPoint = new IPEndPoint(stunServerIP, settings.StunPort);

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
