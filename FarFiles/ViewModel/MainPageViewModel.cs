//JEEWEE
//using Android.Media;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using FarFiles.Services;
using Microsoft.Maui.Controls;

//JEEWEE
//using Java.Time.Chrono;
using STUN.Client;
using STUN.Enums;
using System;
//JEEWEE
//using Java.Util;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

    protected int _numSendMsg = 0;
    protected int _numReceivedAns = 0;
    protected UdpClient _udpClient = null;
    protected FileDataService _fileDataService;
    protected CopyMgr _copyMgr = null;
    protected PathInfoAnswerState _currPathInfoAnswerState = null;
    protected int _seqNrPathInfoAns = 0;
    protected Thread _threadAndroidPathInfo = null;
    protected FileOrFolderData[] _fileOrFolderDataArrayOnSvr = null;


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
    // And for FullPathRoot and Idx0isSvr1isCl an extra measure is necessary:
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
            f => new FileOrFolderData(f, true, 0)).ToArray();
        MauiProgram.Info.CurrSvrFiles = fileNames.Order().Select(
            f => new FileOrFolderData(f, false, 0)).ToArray();
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
        //await MauiProgram.Tests.DoTestsWindowsAsync(_fileDataService);
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
                await SndFromClientRecievePathInfo_msgbxs_Async(null);

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
            LblInfo1 = "Error occurred";
            LblInfo2 = "";
            IsBusy = false;

        }
    }


    /// <summary>
    /// Send MauiProgram.Info.SvrPathParts to server, receive folders and files,
    /// set those in MauiProgram.Info.CurrSvrFolders, MauiProgram.Info.CurrSvrFiles
    /// </summary>
    /// <returns></returns>
    /// <param name="funcPathInfoGetAbortSetLbls">returns true if user aborted</param>
    /// <exception cref="Exception"></exception>
    public async Task SndFromClientRecievePathInfo_msgbxs_Async(
                Func<int,bool> funcPathInfoGetAbortSetLbls = null)
    {
        MsgSvrClBase msgSvrCl;
        
        msgSvrCl = new MsgSvrClPathInfoRequest(MauiProgram.Info.SvrPathParts);

        var lisFolders = new List<string>();
        var lisFiles = new List<string>();
        var lisSizes = new List<long>();

        bool abort = false;
        int sleepMilliSecs = 1000;

        LblInfo1 = "sending path info request to server ...";
        while (true)
        {
            int seqNr;
            byte[] byRecieved = await SndFromClientRecieve_msgbxs_Async(
                                msgSvrCl.Bytes);
            LblInfo1 = "";
            if (byRecieved.Length == 0)
                return;
            LblInfo2 = "receiving path info from server ...";

            MsgSvrClBase msgSvrClAnswer = MsgSvrClBase.CreateFromBytes(byRecieved);
            if (msgSvrClAnswer is MsgSvrClAbortedConfirmation)
                break;

            if (msgSvrClAnswer is MsgSvrClPathInfoAndroidBusy)
            {
                ((MsgSvrClPathInfoAndroidBusy)msgSvrClAnswer).GetSeqnr(out seqNr);
                Thread.Sleep(sleepMilliSecs);
                sleepMilliSecs = Math.Min(3000, sleepMilliSecs + 1000);
                LblInfo1 = "sending still busy inquiry to server ...";
                msgSvrCl = new MsgSvrClPathInfoAndroidStillBusyInq();
            }
            else
            {
                msgSvrClAnswer.CheckExpectedTypeMaybeThrow(typeof(MsgSvrClPathInfoAnswer));
                Log($"client: received bytes: {byRecieved.Length}, MsgSvrClPathInfoAnswer");

                ((MsgSvrClPathInfoAnswer)msgSvrClAnswer).GetSeqnrAndIslastAndFolderAndFileNamesAndSizes(
                        out seqNr, out bool isLast, out string[] folderNames, out string[] fileNames, out long[] fileSizes);
                lisFolders.AddRange(folderNames);
                lisFiles.AddRange(fileNames);
                lisSizes.AddRange(fileSizes);

                if (isLast)
                {
                    LblInfo2 = "received: path info from server";
                    break;
                }
            }

            if (null != funcPathInfoGetAbortSetLbls)
            {
                if (funcPathInfoGetAbortSetLbls(seqNr))
                {
                    LblInfo1 = "sending aborted info to server ...";
                    msgSvrCl = new MsgSvrClAbortedInfo();
                    abort = true;
                    // we must still send aborted info to the server, and receive confirmation
                }
            }

            if (!abort)
            {
                LblInfo1 = "sending path info request to server ...";
                msgSvrCl = new MsgSvrClPathInfoNextpartRequest();
            }
        }

        if (! abort)
        {
            MauiProgram.Info.CurrSvrFolders = lisFolders.Order().Select(
                f => new FileOrFolderData(f, true, 0)).ToArray();
            int i = 0;
            MauiProgram.Info.CurrSvrFiles = lisFiles.Select(
                f => new FileOrFolderData(f, false, lisSizes[i++]))
                .OrderBy(f => f.Name)
                .ToArray();
        }
    }


    public async Task CopyFromSvr_msgbxs_Async(FileOrFolderData[] selecteds,
            Func<int,int,long,long,bool> funcCopyGetAbortSetLbls = null)
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
                if (msgSvrClAnswer is MsgSvrClAbortedConfirmation)
                    break;

                msgSvrClAnswer.CheckExpectedTypeMaybeThrow(typeof(MsgSvrClCopyAnswer));
                Log($"client: received bytes: {byRecieved.Length}, MsgSvrClCopyAnswer");

                if (copyMgr.CreateOnClientFromNextPart((MsgSvrClCopyAnswer)msgSvrClAnswer,
                                                funcCopyGetAbortSetLbls))
                    break;          // ready

                if (copyMgr.ClientAborted)
                    msgSvrCl = new MsgSvrClAbortedInfo();
                else
                    msgSvrCl = new MsgSvrClCopyNextpartRequest();
            }

            string nl = Environment.NewLine;
            await Shell.Current.DisplayAlert("Copied",
                $"Folders created: {copyMgr.NumFoldersCreated}{nl}" +
                $"Files created: {copyMgr.NumFilesCreated}{nl}" +
                $"Files overwritten: {copyMgr.NumFilesOverwritten}{nl}" +
                $"Files skipped: {copyMgr.NumFilesSkipped}{nl}" +
                (copyMgr.NumDtProblems > 0 ? $"Err dates replaced by Now: {copyMgr.NumDtProblems}{nl}" : "") +
                (copyMgr.ErrMsgs.Count > 0 ? $"ERRORS: {copyMgr.ErrMsgs.Count}{nl}" : "") +
                (copyMgr.ClientAborted ? $"ABORTED BY USER{nl}" : ""),
                "OK");

            if (copyMgr.ErrMsgs.Count > 0)
            {
                MauiProgram.Log.LogLine("", false);
                MauiProgram.Log.LogLine("CopyMgr ErrMsgs:", false);
                foreach (string errMsg in copyMgr.ErrMsgs)
                {
                    MauiProgram.Log.LogLine(errMsg, false);
                }

                int maxDispMsgs = 5;
                int numMsgs = Math.Min(maxDispMsgs, copyMgr.ErrMsgs.Count);
                string totalMsg = "";
                for (int iErr = 0; iErr < numMsgs; iErr++)
                    totalMsg += copyMgr.ErrMsgs[iErr] + nl;

                if (copyMgr.ErrMsgs.Count > maxDispMsgs)
                    totalMsg += $"....... (not all {copyMgr.ErrMsgs.Count} errors listed)";
                await Shell.Current.DisplayAlert("ERRORS", totalMsg, "OK");
            }
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
        try
        {
            //JEEWEE
            //LblInfo2 = "";
            //LblInfo1 = $"sending to server: msg {++_numSendMsg}, {sendBytes.Length} bytes ...";
            int iResult = await _udpClient.SendAsync(sendBytes, sendBytes.Length);

            //JEEWEE
            //LblInfo2 = $"sent to server: msg {_numSendMsg}, {iResult} bytes, waiting for server ...";
            Log($"client: sent to server: msg {++_numSendMsg}, {iResult} bytes, waiting for server...");

            UdpReceiveResult response = await _udpClient.ReceiveAsync(
                                new CancellationTokenSource(
                                    MauiProgram.Settings.TimeoutSecsClient * 1000).Token);

            //JEEWEE
            //LblInfo2 = $"Received from server: {Encoding.UTF8.GetString(response.Buffer)}'";
            //LblInfo2 = $"Received from server: answer {++_numReceivedAns}, {response.Buffer.Length} bytes";
            Log($"Received from server: answer {++_numReceivedAns}, {response.Buffer.Length} bytes");

            return response.Buffer;
        }
        catch (OperationCanceledException)
        {
            string errMsg = $"Response from server timed out";
            await Shell.Current.DisplayAlert("Error", errMsg, "OK");
        }
        catch (Exception exc)
        {
            Log("client: exception; LblInfo1=" + LblInfo1);
            Log("client: exception; LblInfo2=" + LblInfo2);
            string errMsg = $"Unable to receive from server: {MauiProgram.ExcMsgWithInnerMsgs(exc)}";
            await Shell.Current.DisplayAlert("Error", errMsg, "OK");
        }

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

            //JEEWEE
            //string receivedTxt = "";
            //string errToSendTxt = "";

            // we should close _reader if for any reason another message comes in than as expected
            if (null != _copyMgr && !(msgSvrCl is MsgSvrClCopyNextpartRequest))
            {
                _copyMgr.Dispose();
                _copyMgr = null;
            }

            string sendWhatStr = "";

            // React on different types of messages:
            if (msgSvrCl is MsgSvrClStringSend)
            {
                string receivedTxt = ((MsgSvrClStringSend)msgSvrCl).GetString();
                LblInfo1 = $"received: string: '{receivedTxt}'";
                msgSvrClAns = new MsgSvrClStringAnswer();
                sendWhatStr = "answer";
            }
            else if (msgSvrCl is MsgSvrClPathInfoRequest)
            {
                //JEEWEE
                //receivedTxt = msgSvrCl.Type.ToString();
                _seqNrPathInfoAns = 0;

                string[] svrSubParts = ((MsgSvrClPathInfoRequest)msgSvrCl).GetSvrSubParts();

                LblInfo1 = "received: path info request, relpath='" +
                        String.Join("/", svrSubParts) + "'";
#if ANDROID
                if (null != _threadAndroidPathInfo &&
                        _threadAndroidPathInfo.ThreadState == System.Threading.ThreadState.Running)
                {
                    msgSvrClAns = new MsgSvrClErrorAnswer(
                        $"Server: path info request: not yet possible because of aborted previous request");
                    sendWhatStr = "ERRORMSG (previous aborted thread still active)";
                }
                else
                {
                    _threadAndroidPathInfo = new Thread(() => GetFileOrFolderDataArray(
                                svrSubParts));
                    _threadAndroidPathInfo.Start();
                    msgSvrClAns = new MsgSvrClPathInfoAndroidBusy(_seqNrPathInfoAns++);
                }

#else
                //JEEWEE
                //FileOrFolderData[] data = _fileDataService.GetFilesAndFoldersDataGeneric(
                //            Settings.FullPathRoot,
                //            Settings.AndroidUriRoot,
                //            svrSubParts);

                // on Windows there is no performance trouble
                GetFileOrFolderDataArray(svrSubParts);
                msgSvrClAns = HandleFileOrFolderDataArrayOnSvr(out sendWhatStr);
#endif

            }
            else if (msgSvrCl is MsgSvrClPathInfoAndroidStillBusyInq)
            {
                LblInfo1 = "received: query is Android still busy";
                if (_threadAndroidPathInfo.ThreadState ==
                            System.Threading.ThreadState.Running)
                {
                    msgSvrClAns = new MsgSvrClPathInfoAndroidBusy(_seqNrPathInfoAns++);
                    sendWhatStr = $"still busy ({_seqNrPathInfoAns})";
                }
                else
                {
                    msgSvrClAns = HandleFileOrFolderDataArrayOnSvr(out sendWhatStr);
                }
            }
            else if (msgSvrCl is MsgSvrClPathInfoNextpartRequest)
            {
                LblInfo1 = "received: request next part path info";
                if (null == _currPathInfoAnswerState)
                {
                    //JEEWEE
                    //errToSendTxt =
                    //    $"Server: wrong request last {msgSvrCl.GetType()}, no active path info answer state";
                    //msgSvrClAns = new MsgSvrClErrorAnswer(errToSendTxt);

                    msgSvrClAns = new MsgSvrClErrorAnswer(
                        $"Server: wrong request last {msgSvrCl.GetType()}, no active path info answer state");
                    sendWhatStr = "ERRORMSG (wrong request next part path info)";
                }
                else
                {
                    msgSvrClAns = new MsgSvrClPathInfoAnswer(_seqNrPathInfoAns++,
                            _currPathInfoAnswerState,
                            MauiProgram.Settings.BufSizeMoreOrLess);
                    sendWhatStr = "path info part " + _seqNrPathInfoAns;
                }
            }
            else if (msgSvrCl is MsgSvrClCopyRequest)
            {
                LblInfo1 = "received: copy request";
                _copyMgr = new CopyMgr(_fileDataService);
                _copyMgr.StartCopyFromSvr((MsgSvrClCopyRequest)msgSvrCl);
                msgSvrClAns = _copyMgr.GetNextPartCopyansFromSvr();
                sendWhatStr = SendWhatStrFromCopyMsgtosend(msgSvrClAns);
            }
            else if (msgSvrCl is MsgSvrClCopyNextpartRequest)
            {
                if (null == _copyMgr)
                {
                    //JEEWEE
                    //errToSendTxt =
                    //    $"Server: wrong request last {msgSvrCl.GetType()}, no active copy process";
                    //msgSvrClAns = new MsgSvrClErrorAnswer(errToSendTxt);
                    msgSvrClAns = new MsgSvrClErrorAnswer(
                        $"Server: wrong request last {msgSvrCl.GetType()}, no active copy process");
                    sendWhatStr = $"ERRORMSG (wrong request {msgSvrCl.GetType()})";
                }
                else
                {
                    LblInfo1 = "received: next part copy request";
                    msgSvrClAns = _copyMgr.GetNextPartCopyansFromSvr();
                    if (msgSvrClAns is MsgSvrClCopyAnswer &&
                        ((MsgSvrClCopyAnswer)msgSvrClAns).IsLastPart)
                    {
                        _copyMgr.Dispose();
                        _copyMgr = null;
                    }
                    sendWhatStr = SendWhatStrFromCopyMsgtosend(msgSvrClAns);
                }
            }
            else if (msgSvrCl is MsgSvrClAbortedInfo)
            {
                LblInfo1 = "received: info that client aborted";
                msgSvrClAns = new MsgSvrClAbortedConfirmation();
                // JWdP 20250530 _threadAndroidPathInfo.Abort() is obsolete and
                // raises PlatformNotSupported exception, it says, so I just let it terminating
                sendWhatStr = "confirmation";
            }
            else
            {
                //JEEWEE
                //errToSendTxt =
                //    $"Server: received unexpected message type {msgSvrCl.GetType()}";
                //msgSvrClAns = new MsgSvrClErrorAnswer(errToSendTxt);

                msgSvrClAns = new MsgSvrClErrorAnswer(
                    $"Server: received unexpected message type {msgSvrCl.GetType()}");
                sendWhatStr = $"ERRORMSG (wrong request {msgSvrCl.GetType()})";
            }

            //JEEWEE
            //LblInfo1 = $"Received from client: '{receivedTxt}'";

            //5️ Respond to client(hole punching)
            //JEEWEE
            //int numAnswer = MauiProgram.Info.NumAnswersSent + 1;
            
            LblInfo2 = $"sending {sendWhatStr} ...";
            Log($"server: going to send bytes: {msgSvrClAns.Bytes.Length}, {msgSvrClAns.GetType()}");
            await udpServer.SendAsync(msgSvrClAns.Bytes, msgSvrClAns.Bytes.Length,
                        received.RemoteEndPoint);
            //JEEWEE
            //LblInfo2 = $"answer {numAnswer} sent: " +
            //    (!String.IsNullOrEmpty(errToSendTxt) ? errToSendTxt :
            //    $"{msgSvrClAns.Bytes.Length} bytes");
            LblInfo2 = $"sent: {sendWhatStr}";

            //JEEWEE
            //MauiProgram.Info.NumAnswersSent = numAnswer;
            MauiProgram.Info.NumAnswersSent++;
        }
        catch (Exception exc)
        {
            Log($"server: EXCEPTION: ListenMsgAndSendMsgAsSvrAsync: " +
                    MauiProgram.ExcMsgWithInnerMsgs(exc));
            await Shell.Current.DisplayAlert("Server: error receiving message or sending answer",
                        MauiProgram.ExcMsgWithInnerMsgs(exc), "OK");
        }
    }


    protected void GetFileOrFolderDataArray(string[] svrSubParts)
    {
        _fileOrFolderDataArrayOnSvr = _fileDataService.GetFilesAndFoldersDataGeneric(
                    Settings.FullPathRoot,
                    Settings.AndroidUriRoot,
                    svrSubParts);
    }

    protected MsgSvrClBase HandleFileOrFolderDataArrayOnSvr(out string sendWhatStr)
    {
        MsgSvrClBase msgSvrClAns;
        var dataWithExc = _fileOrFolderDataArrayOnSvr.Where(d => null != d.ExcThrown)
                    .FirstOrDefault();
        if (dataWithExc != null)
        {
            _currPathInfoAnswerState = null;
            msgSvrClAns = new MsgSvrClErrorAnswer(dataWithExc.ExcThrown.Message);
            sendWhatStr = $"ERRORMSG ({dataWithExc.ExcThrown.Message})";
        }
        else
        {
            string[] folderNames = _fileOrFolderDataArrayOnSvr.Where(
                        d => d.IsDir).Select(d => d.Name).ToArray();
            string[] fileNames = _fileOrFolderDataArrayOnSvr.Where(
                        d => !d.IsDir).Select(d => d.Name).ToArray();
            long[] fileSizes = _fileOrFolderDataArrayOnSvr.Where(
                        d => !d.IsDir).Select(d => d.FileSize).ToArray();
            _currPathInfoAnswerState = new PathInfoAnswerState(folderNames,
                        fileNames, fileSizes);
            msgSvrClAns = new MsgSvrClPathInfoAnswer(_seqNrPathInfoAns++,
                    _currPathInfoAnswerState,
                    MauiProgram.Settings.BufSizeMoreOrLess);
            sendWhatStr = "path info " +
                    (_currPathInfoAnswerState.EndReached ?
                    "last part" : $"part {_seqNrPathInfoAns}");
        }

        return msgSvrClAns;
    }


    protected string SendWhatStrFromCopyMsgtosend(MsgSvrClBase msgSvrClAns)
    {
        if (msgSvrClAns is MsgSvrClErrorAnswer)
            return $"ERRORMSG ({((MsgSvrClErrorAnswer)msgSvrClAns).GetErrMsg()})";

        if (msgSvrClAns is MsgSvrClCopyAnswer)
            return "copy info: " +
                (((MsgSvrClCopyAnswer)msgSvrClAns).IsLastPart ? "last part" :
                $"copy info; file {_copyMgr?.NumFilesOpenedOnSvr} of {_copyMgr?.TotalNumFilesToCopyOnSvr}");

        // should not happen:
        return msgSvrClAns.GetType().ToString();
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
