using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Storage;
using FarFiles.Services;
using Microsoft.Maui.Controls;

using STUN.Client;
using STUN.Enums;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;

//JEEWEE
//using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Channels;
//JEEWEE
//using Windows.Media.Protection.PlayReady;

namespace FarFiles.ViewModel;

public partial class MainPageViewModel : BaseViewModel
{
    protected int _numSendMsg = 0;
    protected int _numLogRcvans = 0;
    protected int _numDispRcvmsg = 0;
    protected CommunicWrapper _communicClient = null;
    protected CommunicWrapper _communicServer = null;

    protected FileDataService _fileDataService;
    protected CopyMgr _copyMgr = null;

    protected bool _rememberClientRemoteEnd = true;
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

        if (MauiProgram.Settings.LastKnownState != FfState.UNREGISTERED)
        {
            LblInfo1 = "Last time, app was still registered in Central Server; consider closing app with red button 'X'";
        }
    }

    // xaml cannot bind to MauiProgram.Settings directly.
    // And for FullPathRoot and SvrClModeAsInt an extra measure is necessary:
    public Settings Settings { get; protected set; } = MauiProgram.Settings;

    // ... and now SvrClModeAsInt needs even an extra in-between to manage
    // selection change:
    public int SvrClModeAsInt
    {
        get => MauiProgram.Settings.SvrClModeAsInt;
        set
        {
            if (MauiProgram.Settings.SvrClModeAsInt != value)
            {
                MauiProgram.Settings.SvrClModeAsInt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCommunicModeVisible));
            }
        }
    }

    // same thing for CommunicModeAsInt
    // selection change:
    public int CommunicModeAsInt
    {
        get => MauiProgram.Settings.CommunicModeAsInt;
        set
        {
            if (MauiProgram.Settings.CommunicModeAsInt != value)
            {
                MauiProgram.Settings.CommunicModeAsInt = value;
                OnPropertyChanged();
            }
        }
    }


    public string Ffstate_imgsrc { get =>
                MauiProgram.Info.FfState == FfState.UNREGISTERED ? "ffstate_unreg.gif" :
                (MauiProgram.Info.FfState == FfState.REGISTERED ? "ffstate_reg.gif" :
                (MauiProgram.Info.FfState == FfState.CONNECTED ? "ffstate_conn.gif" :
                (MauiProgram.Info.FfState == FfState.INTRANSACTION ? "ffstate_intrans.gif" :
                ""))); }

    public string StrFfstate { get => "State: " + MauiProgram.FirstCapitalRestLowc(MauiProgram.Info.FfState.ToString()); }
    public void SetFfInfoStateAndImage(FfState newFfState)
    {
        MauiProgram.Info.FfState = newFfState;
        MauiProgram.Info.ClientPageVwModel?.InvokeOnPropFfstateImg();
        OnPropertyChanged(nameof(Ffstate_imgsrc));
        OnPropertyChanged(nameof(StrFfstate));
    }


    //JEEWEE
    protected bool _enableConnect = true;
    public bool IsBtnConnectEnabled
    {
        get => IsNotBusy && _enableConnect;
        set => _enableConnect = value;       // in this app implicates:
    }

    protected bool _isBtnConnectVisible = true;
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
                OnPropertyChanged(nameof(IsCommunicModeVisible));
            }
        }
    }

    public bool IsBtnBackToFilesVisible
    {
        get => ! IsBtnConnectVisible;
    }

    public bool IsCommunicModeVisible
    {
        // currently always true
        get => true;
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
            MauiProgram.SaveSettings_donotforgettoaddnewsetting();     // is necessary
            Settings.AndroidUriRoot = await MauiProgram.AndroidFolderPicker.PickFolderAsync();
            var context = global::Android.App.Application.Context;

            // JWdP 20250518 Another necessary ^&*%^%^, provided to me by ChatGPT,
            // else the deserialized AndroidUri from settings results in Invalid Uri, next time.
            context.ContentResolver.TakePersistableUriPermission(
                Settings.AndroidUriRoot,
                Android.Content.ActivityFlags.GrantReadUriPermission |
                Android.Content.ActivityFlags.GrantWriteUriPermission);

            MauiProgram.SaveSettings_donotforgettoaddnewsetting();
            OnPropertyChanged("FullPathRoot");

#else
            var folderPickerResult = await FolderPicker.PickAsync("");
            if (!folderPickerResult.IsSuccessful)
            {
                return;         // almost certainly not an error; user cancelled
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
        //JEEWEE
        //if (MauiProgram.Info.Connected)
        if (MauiProgram.Info.FfState == FfState.CONNECTED)
        {
            CommunicWrapper communicWr = _communicClient ?? _communicServer;
            if (null != communicWr)
            {
                string srvcl = _communicClient == null ? "server" : "client";
                var msgSvrCl = new MsgSvrClDisconnInfo();
                Log($"{srvcl}, OnCloseThings: trying to send MsgSvrClDisconnInfo to other side");
                Task.Run(() => communicWr.SendBytesAsync(msgSvrCl.Bytes, msgSvrCl.Bytes.Length))
                    .Wait(TimeSpan.FromSeconds(1));
            }
        }
        SetFfInfoStateAndImage(FfState.UNREGISTERED);
        _copyMgr?.Dispose();
        _communicServer?.Dispose();
        _communicServer = null;
        _communicClient?.Dispose();
        _communicClient = null;
        //JEEWEE
        IsBtnConnectEnabled = false;
        OnPropertyChanged(nameof(IsBtnConnectEnabled));

        MauiProgram.Log.WriteLogLinesAndroidAsync(_fileDataService);
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

        //await TestUpldDwnldJEEWEE();
        //OpenClientJEEWEE();
        //return;

        if (String.IsNullOrEmpty(MauiProgram.Settings.FullPathRoot))
        {
            await Shell.Current.DisplayAlert("Info",
                "Connect: Please browse for root first", "OK");
            return;
        }
        if (String.IsNullOrEmpty(MauiProgram.Settings.ConnectKey))
        {
            await Shell.Current.DisplayAlert("Info",
                "Connect: Please enter connect key first", "OK");
            return;
        }

        string descrTrying = "";
        try
        {
            IsBusy = true;

            // if ConnClientGuid is not yet in settings, determine it for this device.
            if (Guid.Empty == MauiProgram.Settings.ConnClientGuid)
            {
                MauiProgram.Settings.ConnClientGuid = Guid.NewGuid();
                MauiProgram.SaveSettings_donotforgettoaddnewsetting();
            }

            // JWdP 20251026 I think maybe better use a new Guid for a one session id for communication through Central Server,
            // not the persistent Settings.ConnClientGuid, in case there are two simultaneous copy operations on the same device 
            MauiProgram.Info.IdInsteadOfUdp = Guid.NewGuid()
                .ToString().ToUpper().Replace("-", "");
            if (MauiProgram.Info.IdInsteadOfUdp.Length != 32)
                throw new Exception($"Very weird: length '{MauiProgram.Info.IdInsteadOfUdp}' not 32");

            // Sometimes, later on, we need to know how this instance was connected first time, before any swap
            MauiProgram.Info.FirstModeIsServer = MauiProgram.Settings.ModeIsServer;

            // server and client: get udp port from Stun server. But if communication through
            // central server is used: use MauiProgram.Info.IdInsteadOfUdp
            int udpPort = 0;
            if (MauiProgram.Settings.CommunicModeAsInt != (int)CommunicMode.CENTRALSVR)
            {
                descrTrying = " obtaining udp port from stun server";
                var udpIEndPoint = await GetUdpSvrIEndPointFromStun(Settings);
                if (null == udpIEndPoint)
                    throw new Exception("Unknown error getting data from Stun Server");
                udpPort = udpIEndPoint.Port;
                if (udpPort <= 0)
                    throw new Exception("Wrong data from Stun Server");
                Log($"udpPort from Stun server={udpPort}");
            }
            else
            {
                Log($"no udpPort from Stun server (communication by Central Server)");
            }

            MauiProgram.Info.UdpPort = udpPort;
            descrTrying = " obtaining local IP";
            MauiProgram.Info.StrLocalIP = GetLocalIP();

            descrTrying = " registering in central server";
            string response = await MauiProgram.PostToCentralServerAsync("REGISTER");

            CentralSvrRespToInfoData(response);

            // no exception, so:
            SetFfInfoStateAndImage(FfState.REGISTERED);

            IsBusy = true;

            if (MauiProgram.Settings.ModeIsServer)
            {
                // Polling is not necessary nor desired for localip
                if (MauiProgram.Settings.CommunicModeAsInt != (int)CommunicMode.LOCALIP)
                {
                    // server: in loop: poll central server untill client connects
                    descrTrying = " consulting client connection in central server";
                    await PollCentralServerAsSvrUntilClientConnectsAsync();

                    MauiProgram.Log.LogLine($"server: after polling: CommunicModeAsInt=" +
                            $"{MauiProgram.Settings.CommunicModeAsInt}");
                }

                // server: do conversation: in loop: listen for client msgs and response
                descrTrying = " creating communication object";
                try
                {
                    var udpClientOrNull = MauiProgram.Settings.CommunicModeAsInt == (int)CommunicMode.CENTRALSVR ?
                                null : new UdpClient(udpPort);
                    _communicServer = new CommunicWrapper(udpClientOrNull);
                    Log($"server: created CommunicWrapper with udpClientOrNull=" +
                        (null == udpClientOrNull ? "null" : $"udpPort={udpPort}"));
                }
                catch
                {
                    _communicServer = null;
                    throw;
                }

                if (MauiProgram.Settings.CommunicModeAsInt == (int)CommunicMode.NATHOLEPUNCHING)
                {
                    // send client a dummy message("NAT hole punching")
                    descrTrying = " NAT hole punching";
                    LblInfo1 = $"doing NAT hole punching ...";
                    await DoHolePunchingAsync(_communicServer.UdpWrapper);
                }

                // Do the big listen-answer loop
                LblInfo1 = $"Listening for client contact ...";
                await DoListenLoopAsSvrAsync();

                //JEEWEE
                //if (MauiProgram.Info.AppIsInResetState)
                //{
                //    MauiProgram.Info.AppIsInResetState = false;
                //    return;
                //}
                if (MauiProgram.Info.AppIsShuttingDown)
                    return;

                // if we are here, then now the server/client were swapped, and now
                // we are client
                descrTrying = " starting to act as a client";

                await RetrieveServerPathInfoOnClientAndOpenClientPageAsync();
            }
            else
            {
                // client: connect to server
                descrTrying = " connecting to server";
                string errMsg = await ConnectServerFromClientAsync();
                if ("" != errMsg)
                {
                    throw new Exception(errMsg);
                }

                //JEEWEE: THIS IS TOTALLY WRONG, BUT HELPS IF SERVER POLLS AT 5 SECS (not necessary any more)
                //await Task.Delay(6 * 1000);

                descrTrying = " starting to act as a client";
                _rememberClientRemoteEnd = false;   // also after swap: do not remember RemoteEndPoint
                await RetrieveServerPathInfoOnClientAndOpenClientPageAsync();
            }

            //JEEWEE
            //MauiProgram.Info.Connected = true;
            SetFfInfoStateAndImage(FfState.CONNECTED);
        }
        catch (Exception exc)
        {
            await Shell.Current.DisplayAlert("Error" + descrTrying,
                MauiProgram.ExcMsgWithInnerMsgs(exc), "OK");
            LblInfo1 = "Error occurred";
            LblInfo2 = "";
        }
        finally
        {
            IsBusy = false;
        }
    }



    /// <summary>
    /// Gets Json prop 'ipData' from response and sets it into Info props
    /// Throws exception if there is an errMsg
    /// </summary>
    /// <param name="respFromCentralSvr"></param>
    /// <exception cref="Exception"></exception>
    protected void CentralSvrRespToInfoData(string respFromCentralSvr)
    {
        string errMsg = GetJsonProp(respFromCentralSvr, "errMsg");
        if ("" == errMsg)
            MauiProgram.Info.RegisteredCode = GetJsonProp(respFromCentralSvr, "registeredCode");
        if ("" == errMsg)
            errMsg = IpDataToInfo(GetJsonProp(respFromCentralSvr, "ipData"));

        //JEEWEE
        //if ("" == errMsg && MauiProgram.Settings.ModeIsServer)
        //{
        //    string clientCommunicModeAsStr = GetJsonProp(respFromCentralSvr, "clientCommunicMode");
        //    if (MauiProgram.ParseIntEnum<CommunicMode>(clientCommunicModeAsStr, out int modeAsInt))
        //    {
        //        MauiProgram.Settings.CommunicModeAsInt = modeAsInt;
        //    }
        //}

        if ("" != errMsg)
        {
            throw new Exception(errMsg);
        }
    }


    public async Task PollCentralServerAsSvrUntilClientConnectsAsync()
    {
        int nTimes = 1;
        int sleepMilliSecs = 5000;
        while (true)
        {
            LblInfo1 = $"Inquiring if client connected ({nTimes++}) ...";
            string response = await MauiProgram.PostToCentralServerAsync("GETDATA");
            CentralSvrRespToInfoData(response);
            if (MauiProgram.Info.RegisteredCode == "2")
                return;

            //JEEWEE
            //Thread.Sleep(sleepMilliSecs);
            await Task.Delay(sleepMilliSecs);
        }
    }



    /// <summary>
    /// Starts a listen-and-answer-loop as server; stay in loop unless something
    /// weird happens, or a swap request from client was received and accepted,
    /// or the swap request that was just made as a client (and we swapped to Server)
    /// was rejected and we now swapped back to Client
    /// </summary>
    /// <returns>true if we must go on as a client, false if something weird ended the loop</returns>
    public async Task DoListenLoopAsSvrAsync()
    {
        IsBusy = true;
        while (true)
        {
            if (await ListenMsgAndSendMsgOnSvrAsync())
                break;
        }
    }


    protected async Task RetrieveServerPathInfoOnClientAndOpenClientPageAsync()
    {
        LblInfo2 += "; trying to retrieve server path info";

        // client: send request info rootpath and recieve answer
        // if connection fails (for firewall for example) there is a timeout
        // and receivedPathInfo is false
        bool receivedPathInfo = await SndFromClientRecievePathInfo_msgbxs_Async(null);
        if (receivedPathInfo)
        {
            await Shell.Current.GoToAsync(nameof(ClientPage), true);
            IsBtnConnectVisible = false;
        }
        else
        {
            DisconnectAndDisableOnClient();
            LblInfo2 = "retrieving server path info failed";
        }
    }





    /// <summary>
    /// Send MauiProgram.Info.SvrPathParts to server, receive folders and files,
    /// set those in MauiProgram.Info.CurrSvrFolders, MauiProgram.Info.CurrSvrFiles
    /// </summary>
    /// <returns>true for received path info or aborted, false for problem</returns>
    /// <param name="funcPathInfoGetAbortSetLbls">returns true if user aborted</param>
    /// <exception cref="Exception"></exception>
    public async Task<bool> SndFromClientRecievePathInfo_msgbxs_Async(
                Func<int,bool> funcPathInfoGetAbortSetLbls = null)
    {
        MsgSvrClBase msgSvrClToSend;
        
        msgSvrClToSend = new MsgSvrClPathInfoRequest(MauiProgram.Settings.ConnClientGuid,
                                MauiProgram.Info.SvrPathParts);

        var lisFolders = new List<string>();
        var lisFiles = new List<string>();
        var lisSizes = new List<long>();

        bool abort = false;
        int sleepMilliSecs = 1000;

        LblInfo1 = "sending path info request to server ...";
        while (true)
        {
            int seqNr;

            LblInfo2 = "receiving path info from server ...";
            MsgSvrClBase msgSvrClAnswer = await SndFromClientRecieve_msgbxs_Async(
                                msgSvrClToSend);
            if (msgSvrClAnswer == null)
                return false;   //JEEWEE
            if (msgSvrClAnswer is MsgSvrClAbortedConfirmation)
            {
                SetFfInfoStateAndImage(FfState.CONNECTED);
                break;
            }

            if (msgSvrClAnswer is MsgSvrClPathInfoAndroidBusy)
            {
                ((MsgSvrClPathInfoAndroidBusy)msgSvrClAnswer).GetSeqnr(out seqNr);
                Thread.Sleep(sleepMilliSecs);
                sleepMilliSecs = Math.Min(3000, sleepMilliSecs + 1000);
                LblInfo1 = "sending still busy inquiry to server ...";
                msgSvrClToSend = new MsgSvrClPathInfoAndroidStillBusyInq();
                SetFfInfoStateAndImage(FfState.INTRANSACTION);
            }
            else
            {
                msgSvrClAnswer.CheckExpectedTypeMaybeThrow(typeof(MsgSvrClPathInfoAnswer));

                ((MsgSvrClPathInfoAnswer)msgSvrClAnswer).GetSeqnrAndIswrAndIslastAndFolderAndFileNamesAndSizes(
                        out seqNr, out bool isSvrWritable, out bool isLast, out string[] folderNames, out string[] fileNames, out long[] fileSizes);
                lisFolders.AddRange(folderNames);
                lisFiles.AddRange(fileNames);
                lisSizes.AddRange(fileSizes);

                MauiProgram.Info.IsSvrWritableReportedToClient = isSvrWritable;

                if (isLast)
                {
                    LblInfo2 = $"({++_numDispRcvmsg}) received: path info from server";
                    SetFfInfoStateAndImage(FfState.CONNECTED);
                    break;
                }
                else
                {
                    SetFfInfoStateAndImage(FfState.INTRANSACTION);
                }
            }

            if (null != funcPathInfoGetAbortSetLbls)
            {
                if (funcPathInfoGetAbortSetLbls(seqNr))
                {
                    LblInfo1 = "sending aborted info to server ...";
                    msgSvrClToSend = new MsgSvrClAbortedInfo();
                    abort = true;
                    // we must still send aborted info to the server, and receive confirmation
                }
            }

            if (!abort)
            {
                LblInfo1 = "sending request to server ...";
            }
        }
        // PathInfo is complete, or progress was aborted

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

        return true;    // also if aborted
    }



    
    public async Task<bool> SndFromClientRecieveSwapReq_msgbxs_Async()
    {
        MsgSvrClBase msgSvrClToSend;

        msgSvrClToSend = new MsgSvrClSwapRequest();

        MsgSvrClBase msgSvrClAnswer = await SndFromClientRecieve_msgbxs_Async(
                            msgSvrClToSend);
        if (msgSvrClAnswer == null)
            return false;

        LblInfo2 = "receiving recieve confirmation from server ...";

        // should recieve MsgSvrClSwapReqReceivedConfirm; on server now
        // appears a dialog to see whether they agree

        LblInfo2 = $"({++_numDispRcvmsg}) received recieve confirmation from server";
        SetFfInfoStateAndImage(FfState.INTRANSACTION);

        return true;
    }


    /// <summary>
    /// If this is client: change to server. And v.v.
    /// </summary>
    public void SwapSvrClient()
    {
        SvrClModeAsInt = MauiProgram.Settings.ModeIsServer ?
                (int)SvrClMode.CLIENT : (
                    MauiProgram.Info.IsSvrWritableReportedToClient ?
                    (int)SvrClMode.SERVERWRITABLE : (int)SvrClMode.SERVER);
        OnPropertyChanged();

        // we must keep using our own UdpWrapper instance
        var sav = _communicClient;
        _communicClient = _communicServer;
        _communicServer = sav;

        LblInfo1 = "";
        LblInfo2 = "";

        // we restart with empty subpaths
        MauiProgram.Info.SvrPathParts.Clear();
        MauiProgram.Info.LocalPathPartsCl.Clear();

        MauiProgram.Info.CpClientToFromMode = CpClientToFromMode.CLIENTFROMSVR;
        if (null != MauiProgram.Info.ClientPageVwModel)
            MauiProgram.Info.ClientPageVwModel.MoreButtonsMode = false;
        IsBtnConnectVisible = MauiProgram.Settings.ModeIsServer;    // sets also IsBtnBackToFilesVisible
    }



    protected void DisconnectAndDisableOnClient()
    {
        // JWdP 20251101 it is really a problem to try to make things restartable in a way that
        // works on Windows as well as Android; disable Connect button. They can close app and restart.
        MauiProgram.Info.DisconnectOnClient();
        IsBtnConnectVisible = true;
        IsBtnConnectEnabled = false;
        OnPropertyChanged();
    }


    public async Task CopyFromOrToSvrOnClient_msgbxs_Async(
            CpClientToFromMode copyToFromSvrMode,
            FileOrFolderData[] selecteds,
            Func<int, int, long, long, bool> funcCopyGetAbortSetLbls = null)
    {
        IEnumerable<string> selectedDirs = selecteds.Where(f => f.IsDir).Select(f => f.Name);
        IEnumerable<string> selectedFiles = selecteds.Where(f => !f.IsDir).Select(f => f.Name);

        FfState savFfState = MauiProgram.Info.FfState;
        SetFfInfoStateAndImage(FfState.INTRANSACTION);

        try
        {
            MsgSvrClBase msgSvrCl = null;
            if (copyToFromSvrMode == CpClientToFromMode.CLIENTFROMSVR)
            {
                msgSvrCl = new MsgSvrClCopyRequest(MauiProgram.Info.SvrPathParts,
                        selectedDirs, selectedFiles);
            }

            using (var copyMgr = new CopyMgr(_fileDataService))
            {
                var nums = new CopyCounters();
                int numErrMsgsSvr = 0;
                int numErrMsgsClient = 0;
                string firstErrMsgSvr = "";
                string firstErrMsgClient = "";

                if (copyToFromSvrMode == CpClientToFromMode.CLIENTTOSVR)
                {
                    var reqToClientItself = new MsgSvrClCopyRequest(
                            MauiProgram.Info.SvrPathParts,
                            selectedDirs, selectedFiles);
                    copyMgr.StartCopyFromOrToSvrOnSvrOrClient(reqToClientItself,
                            MauiProgram.Info.LocalPathPartsCl.ToArray());
                    msgSvrCl = (MsgSvrClCopyToSvrPart)copyMgr.GetNextPartCopyansFromSrc(
                                    true, funcCopyGetAbortSetLbls);
                }

                while (true)
                {
                    MsgSvrClBase msgSvrClRecieved = await SndFromClientRecieve_msgbxs_Async(
                                    msgSvrCl);
                    if (null == msgSvrClRecieved)
                    {
                        //exception msg was displayed
                        copyMgr.ClientAbort(copyToFromSvrMode == CpClientToFromMode.CLIENTTOSVR);
                        return;
                    }
                    if (msgSvrClRecieved is MsgSvrClAbortedConfirmation)
                        break;

                    Type expectedType = copyToFromSvrMode == CpClientToFromMode.CLIENTFROMSVR ?
                        typeof(MsgSvrClCopyAnswer) : typeof(MsgSvrClCopyToSvrConfirmation);
                    msgSvrClRecieved.CheckExpectedTypeMaybeThrow(expectedType);

                    if (copyToFromSvrMode == CpClientToFromMode.CLIENTFROMSVR)
                    {
                        if (copyMgr.CreateOnDestFromNextPart((MsgSvrClCopyAnswer)msgSvrClRecieved,
                                                        funcCopyGetAbortSetLbls))
                        {
                            // ready
                            copyMgr.LogErrMsgsIfAny("client ErrMsgs:");
                            nums = copyMgr.Nums;
                            numErrMsgsClient = copyMgr.ErrMsgs.Count;
                            firstErrMsgClient = copyMgr.FirstErrMsg;
                            break;
                        }
                    }

                    if (copyMgr.ClientAborted)
                        msgSvrCl = new MsgSvrClAbortedInfo();
                    else if (copyToFromSvrMode == CpClientToFromMode.CLIENTFROMSVR)
                        msgSvrCl = new MsgSvrClCopyNextpartRequest();
                    else
                    {
                        // copying from client to server
                        if (((MsgSvrClCopyAnswer)msgSvrCl).IsLastPart)
                        {
                            ((MsgSvrClCopyToSvrConfirmation)msgSvrClRecieved).GetNumsAndFirstErrMsg(
                                out nums, out numErrMsgsSvr, out firstErrMsgSvr);
                            break;
                        }
                        msgSvrCl = copyMgr.GetNextPartCopyansFromSrc(true,
                                funcCopyGetAbortSetLbls);
                    }
                }

                string nl = Environment.NewLine;

                await Shell.Current.DisplayAlert("Copied",
                    $"Folders created: {nums.FoldersCreated}{nl}" +
                    $"Files created: {nums.FilesCreated}{nl}" +
                    $"Files overwritten: {nums.FilesOverwritten}{nl}" +
                    $"Files skipped: {nums.FilesSkipped}{nl}" +
                    (nums.DtProblems > 0 ? $"Err dates replaced by Now: {nums.DtProblems}{nl}" : "") +
                    (nums.ErrHashesDiff > 0 ? $"Err failed hash checks: {nums.ErrHashesDiff}{nl}" : "") +
                    (numErrMsgsSvr > 0 ? $"ERRORS on server: {numErrMsgsSvr}{nl}" : "") +
                    (numErrMsgsSvr > 0 ? $"  first: '{firstErrMsgSvr}'{nl}" : "") +
                    (numErrMsgsClient > 0 ? $"ERRORS on client: {numErrMsgsClient}{nl}" : "") +
                    (numErrMsgsClient > 0 ? $"  first: '{firstErrMsgClient}'{nl}" : "") +
                    (copyMgr.ClientAborted ? $"ABORTED BY USER{nl}" : ""),
                    "OK");

                if (copyMgr.ErrMsgs.Count > 0)
                {
                    MauiProgram.Log.LogLine("", false);
                    copyMgr.LogErrMsgsIfAny("CopyMgr client ErrMsgs:");

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
        catch
        {
            throw;
        }
        finally
        {
            SetFfInfoStateAndImage(savFfState);
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
        await Shell.Current.GoToAsync(nameof(AdvancedPage), true);
    }


    [RelayCommand]
    async Task OpenAboutDlg()
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(AboutPage), true);
        }
        catch (Exception exc)
        {
            await Shell.Current.DisplayAlert("Error",
                MauiProgram.ExcMsgWithInnerMsgs(exc), "OK");
        }
    }


    [RelayCommand]
    public async Task CloseApp()
    {
        bool accepted = await Shell.Current.DisplayAlert("Shutdown?",
            "OK to shutdown application " + (MauiProgram.Info.FfState == FfState.UNREGISTERED ? "?" :
                         "and unregister in Central Server ? "),
            "OK", "Cancel");
        if (accepted)
        {
            MauiProgram.OnCloseThingsTotally();     // this calls Info.MainPageVwModel.OnCloseThings()
            Application.Current.Quit();
        }
    }


    /// <summary>
    /// Sends bytes to server and receives bytes. If exception, or other side disconnected,
    /// displays alert and returns null
    /// </summary>
    /// <param name="sendMsgSvrCl"></param>
    /// <returns></returns>
    protected async Task<MsgSvrClBase> SndFromClientRecieve_msgbxs_Async(
                MsgSvrClBase sendMsgSvrCl)
    {
        bool retry = true;
        while (retry)
        {
            try
            {
                MsgSvrClBase msgSvrClAnswer;
                while (true)
                {
                    LblInfo1 = "";
                    LblInfo2 = "";
                    Log($"client: sending to server: {sendMsgSvrCl.GetType()}");
                    int iResult = await _communicClient.SendBytesAsync(sendMsgSvrCl.Bytes, sendMsgSvrCl.Bytes.Length);
                    Log($"client: sent to server: msg {++_numSendMsg}, {iResult} bytes, waiting for server...");

                    //JEEWEE
                    //UdpReceiveResult response = await _udpClient.ReceiveAsync(
                    //        MauiProgram.Settings.TimeoutSecsClient);
                    //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! POLLING SECONDS MECHANISM
                    byte[] response = await _communicClient.ReceiveBytesAsync(
                            3, MauiProgram.Settings.TimeoutSecsClient);

                    Log($"Received from server: answer {++_numLogRcvans}, {response.Length} bytes");

                    if (response.Length == 0)
                    {
                        SetFfInfoStateAndImage(FfState.CONNECTED);
                        throw new Exception($"Answer from server seems empty for unknown reason");
                    }

                    msgSvrClAnswer = MsgSvrClBase.CreateFromBytes(response);
                    Log($"client: received from server: type '{msgSvrClAnswer?.GetType()}'");
                    if (null == msgSvrClAnswer)     // message should be ignored
                        continue;

                    if (await OthersideIsDisconnected_msgbox_Async(msgSvrClAnswer))
                        return null;

                    break;
                }

                //JEEWEE
                //SetFfInfoStateAndImage(FfState.CONNECTED);
                return msgSvrClAnswer;
            }
            catch (OperationCanceledException)
            {
                string errMsg = $"Response from server timed out";
                Log($"client: {errMsg}");
                await Shell.Current.DisplayAlert("Error", errMsg, "OK");
            }
            catch (Exception exc)
            {
                string errMsg = $"Unable to receive from server: {MauiProgram.ExcMsgWithInnerMsgs(exc)}";
                Log($"client: exception, LblInfo1='{LblInfo1}', LblInfo2='{LblInfo2}', errMsg='{errMsg}'");
                await Shell.Current.DisplayAlert("Error", errMsg, "OK");
            }

            SetFfInfoStateAndImage(FfState.REGISTERED);
            retry = await Shell.Current.DisplayAlert("Again?",
                $"Do you want to retry?",
                "Yes", "No");
        }

        DisconnectAndDisableOnClient();

        return null;
    }







    /// <summary>
    /// Server: listen until receiving msg from client, send the answer (so that client does not
    /// timeout). Returns false to stay in the loop, true if client/server were swapped or
    /// a fatal exception was thrown or application is shutting down
    /// </summary>
    /// <returns></returns>
    protected async Task<bool> ListenMsgAndSendMsgOnSvrAsync()
    {
        MsgSvrClBase msgSvrCl = null;

        try
        {
            byte[] received;
            if (null == _communicServer)
                return true;

            try
            {
                // use timeouts of 1 minute
                //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! POLLING MECHANISM
                //received = await _udpServer.ReceiveAsync(60);
                received = await _communicServer.ReceiveBytesAsync(3, 60);
            }
            catch (OperationCanceledException)
            {
                return MauiProgram.Info.AppIsShuttingDown;  // true to break loop, false to listen another minute
            }

            // Client can restart, and connect another time; in that case its IPEndPoint changes.
            // If _udpServer.IPEndPoint is null, client/server were swapped and then the
            // receiver's IPEndPoint must NOT be used
            if (_rememberClientRemoteEnd || _communicServer.UdpWrapper?.IPEndPointClient != null)
            {
                _communicServer.SetClientRemoteEnd(_communicServer.LastReceivedRemoteEndPoint);
                _rememberClientRemoteEnd = false;
            }

            msgSvrCl = MsgSvrClBase.CreateFromBytes(received);
            //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1
            Log($"server: received: {received.Length} bytes: msgSvrCl is '{msgSvrCl}'");
            if (null == msgSvrCl)   // message should be ignored
            {
                return false;       // stay in loop
            }

            MsgSvrClBase msgSvrClAns = null;

            string sendWhatStr = "";

            // React on different types of messages:
            if (await OthersideIsDisconnected_msgbox_Async(msgSvrCl))
            {
                //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! IF CENTRAL SERVER THEN SVR MUST GO BACK INQUIRING
                //return false;       // stay in loop
                //JEEWEE volgens mij niet
                return true;        // quit loop
            }

            if (msgSvrCl is MsgSvrClStringSend)
            {
                string receivedTxt = ((MsgSvrClStringSend)msgSvrCl).GetString();
                LblInfo1 = $"({++_numDispRcvmsg}) received: string: '{receivedTxt}'";
                msgSvrClAns = new MsgSvrClStringAnswer();
                sendWhatStr = "answer";
                SetFfInfoStateAndImage(FfState.CONNECTED);
            }
            else if (msgSvrCl is MsgSvrClPathInfoRequest)
            {
                _seqNrPathInfoAns = 0;

                string[] svrSubParts = ((MsgSvrClPathInfoRequest)msgSvrCl).GetConnclientguidAndSvrSubParts(
                    out Guid connClientGuid);

                if (Guid.Empty == MauiProgram.Info.SvrReceivedClientGuid)
                {
                    MauiProgram.Info.SvrReceivedClientGuid = connClientGuid;
                }

                LblInfo1 = $"({++_numDispRcvmsg}) received: path info request, relpath='" +
                            String.Join("/", svrSubParts) + "'";

                if (connClientGuid != MauiProgram.Info.SvrReceivedClientGuid)
                {
                    msgSvrClAns = new MsgSvrClErrorAnswer(
                        $"Server: already connected to different client");
                    sendWhatStr = "ERRORMSG (request from different client)";
                    SetFfInfoStateAndImage(FfState.CONNECTED);
                }
                else
                {

#if ANDROID
                    if (null != _threadAndroidPathInfo &&
                            _threadAndroidPathInfo.ThreadState == System.Threading.ThreadState.Running)
                    {
                        msgSvrClAns = new MsgSvrClErrorAnswer(
                            $"Server: path info request: not yet possible because of aborted previous request");
                        sendWhatStr = "ERRORMSG (previous aborted thread still active)";
                        SetFfInfoStateAndImage(FfState.CONNECTED);
                    }
                    else
                    {
                        _threadAndroidPathInfo = new Thread(() => GetFileOrFolderDataArray(
                                    svrSubParts));
                        _threadAndroidPathInfo.Start();
                        msgSvrClAns = new MsgSvrClPathInfoAndroidBusy(_seqNrPathInfoAns++);
                        SetFfInfoStateAndImage(FfState.INTRANSACTION);
                    }

#else
                    // on Windows there is no performance trouble
                    GetFileOrFolderDataArray(svrSubParts);
                    msgSvrClAns = HandleFileOrFolderDataArrayOnSvr(out sendWhatStr);
#endif
                }
            }
            else if (msgSvrCl is MsgSvrClPathInfoAndroidStillBusyInq)
            {
                LblInfo1 = $"({++_numDispRcvmsg}) received: inquiry 'is Android still busy'";
                if (_threadAndroidPathInfo.ThreadState ==
                            System.Threading.ThreadState.Running)
                {
                    msgSvrClAns = new MsgSvrClPathInfoAndroidBusy(_seqNrPathInfoAns++);
                    sendWhatStr = $"still busy ({_seqNrPathInfoAns})";
                    SetFfInfoStateAndImage(FfState.INTRANSACTION);
                }
                else
                {
                    msgSvrClAns = HandleFileOrFolderDataArrayOnSvr(out sendWhatStr);
                }
            }
            else if (msgSvrCl is MsgSvrClPathInfoNextpartRequest)
            {
                LblInfo1 = $"({++_numDispRcvmsg}) received: request next part path info";
                if (null == _currPathInfoAnswerState)
                {
                    msgSvrClAns = new MsgSvrClErrorAnswer(
                        $"Server: wrong request last {msgSvrCl.GetType()}, no active path info answer state");
                    sendWhatStr = "ERRORMSG (wrong request next part path info)";
                    SetFfInfoStateAndImage(FfState.CONNECTED);
                }
                else
                {
                    msgSvrClAns = new MsgSvrClPathInfoAnswer(_seqNrPathInfoAns++,
                            Settings.SvrClModeAsInt == (int)SvrClMode.SERVERWRITABLE,
                            _currPathInfoAnswerState,
                            MauiProgram.Settings.BufSizeMoreOrLess);
                    sendWhatStr = "path info part " + _seqNrPathInfoAns;
                    SetFfInfoStateAndImage(_currPathInfoAnswerState.EndReached ?
                                    FfState.CONNECTED : FfState.INTRANSACTION);
                }
            }
            else if (msgSvrCl is MsgSvrClCopyRequest)
            {
                // Start of a copy from svr to client operation
                LblInfo1 = $"({++_numDispRcvmsg}) received: copy request";
                _copyMgr?.Dispose();
                _copyMgr = new CopyMgr(_fileDataService);
                _copyMgr.StartCopyFromOrToSvrOnSvrOrClient(
                    (MsgSvrClCopyRequest)msgSvrCl);
                msgSvrClAns = _copyMgr.GetNextPartCopyansFromSrc(false);
                sendWhatStr = SendWhatStrFromCopyMsgtosend(msgSvrClAns);
            }
            else if (msgSvrCl is MsgSvrClCopyNextpartRequest)
            {
                // request for next part of svr to client operation
                if (null == _copyMgr)
                {
                    msgSvrClAns = new MsgSvrClErrorAnswer(
                        $"Server: wrong request last {msgSvrCl.GetType()}, no active copy process");
                    sendWhatStr = $"ERRORMSG (wrong request {msgSvrCl.GetType()})";
                    SetFfInfoStateAndImage(FfState.CONNECTED);
                }
                else
                {
                    LblInfo1 = $"({++_numDispRcvmsg}) received: next part copy request";
                    msgSvrClAns = _copyMgr.GetNextPartCopyansFromSrc(false);
                    if (msgSvrClAns is MsgSvrClCopyAnswer &&
                        ((MsgSvrClCopyAnswer)msgSvrClAns).IsLastPart)
                    {
                        _copyMgr.LogErrMsgsIfAny("server ErrMsgs:");
                        _copyMgr.Dispose();
                        _copyMgr = null;
                    }
                    sendWhatStr = SendWhatStrFromCopyMsgtosend(msgSvrClAns);
                }
            }
            else if (msgSvrCl is MsgSvrClCopyToSvrPart)
            {
                // Start or next part of a copy TO svr from client operation
                if (null == _copyMgr)
                {
                    _copyMgr = new CopyMgr(_fileDataService);
                    LblInfo1 = $"({++_numDispRcvmsg}) received: start data copy TO server";
                }
                else
                {
                    LblInfo1 = $"({++_numDispRcvmsg}) received: next data copy TO server";
                }

                var msgSvrClCpPart = (MsgSvrClCopyToSvrPart)msgSvrCl;
                bool isLast = _copyMgr.CreateOnDestFromNextPart(msgSvrClCpPart);
                msgSvrClAns = new MsgSvrClCopyToSvrConfirmation(
                    _copyMgr.Nums, _copyMgr.ErrMsgs.Count, _copyMgr.FirstErrMsg);
                if (isLast)
                {
                    _copyMgr.LogErrMsgsIfAny("server ErrMsgs:");
                    _copyMgr.Dispose();
                    _copyMgr = null;
                }
                sendWhatStr = "confirmation";
                SetFfInfoStateAndImage(isLast ? FfState.CONNECTED : FfState.INTRANSACTION);
            }
            else if (msgSvrCl is MsgSvrClAbortedInfo)
            {
                LblInfo1 = $"({++_numDispRcvmsg}) received: info that client aborted";
                msgSvrClAns = new MsgSvrClAbortedConfirmation();
                // JWdP 20250530 _threadAndroidPathInfo.Abort() is obsolete and
                // raises PlatformNotSupported exception, it says, so I just let it terminating
                sendWhatStr = "confirmation";
                SetFfInfoStateAndImage(FfState.CONNECTED);
            }
            else if (msgSvrCl is MsgSvrClSwapRequest)
            {
                // a yes/no dialog is needed, but we cannot let the client timeout,
                // so we already send a received confirmation
                LblInfo1 = $"({++_numDispRcvmsg}) received: request swap server/client";
                msgSvrClAns = new MsgSvrClSwapReqReceivedConfirm();
                sendWhatStr = "swap request recieved confirmation";
                SetFfInfoStateAndImage(FfState.INTRANSACTION);
            }
            else if (msgSvrCl is MsgSvrClSwapRejectedBySvr)
            {
                // client had already swapped, but on server user pressed "Cancel":
                // swap back to Client, and do not send an answer
                LblInfo1 = $"({++_numDispRcvmsg}) received: swap server/client rejected";
                SwapSvrClient();
                SetFfInfoStateAndImage(FfState.CONNECTED);
                return true;
            }
            else
            {
                msgSvrClAns = new MsgSvrClErrorAnswer(
                    $"Server: received unexpected message type {msgSvrCl.GetType()}");
                sendWhatStr = $"ERRORMSG (wrong request {msgSvrCl.GetType()})";
                SetFfInfoStateAndImage(FfState.CONNECTED);
            }

            //5️ Respond to client(hole punching)

            LblInfo2 = $"sending {sendWhatStr} ...";
            Log($"server: going to send bytes: {msgSvrClAns.Bytes.Length}, {msgSvrClAns.GetType()}");
            await _communicServer.SendBytesAsync(msgSvrClAns.Bytes, msgSvrClAns.Bytes.Length);
            LblInfo2 = $"sent: {sendWhatStr}";

            MauiProgram.Info.NumAnswersSent++;

            if (msgSvrCl is MsgSvrClSwapRequest)
            {
                bool accepted = await Shell.Current.DisplayAlert("Swap?",
                    $"Client requested to become server and you to become client. Do you accept?",
                    "OK", "Cancel");
                if (accepted)
                {
                    SwapSvrClient();
                    return true;
                }

                // rejected: send message to the client that now also is server, so that it
                // switches back to client
                msgSvrClAns = new MsgSvrClSwapRejectedBySvr();
                await _communicServer.SendBytesAsync(msgSvrClAns.Bytes, msgSvrClAns.Bytes.Length);
            }
        }
        catch (Exception exc)
        {
            // when the app is closed, an exception is thrown that a thread was terminated or so,
            // we can ignore that (in that case Shell.Current is null)
            if (Shell.Current != null)
            {
                Log($"server: EXCEPTION: ListenMsgAndSendMsgOnSvrAsync: " +
                        MauiProgram.ExcMsgWithInnerMsgs(exc));
                await Shell.Current.DisplayAlert("Server: error receiving message or sending answer",
                            MauiProgram.ExcMsgWithInnerMsgs(exc), "OK");
                SetFfInfoStateAndImage(FfState.CONNECTED);
            }
        }

        return false;
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

        string[] folderNames = _fileOrFolderDataArrayOnSvr.Where(
                    d => d.IsDir).Select(d => d.Name).ToArray();
        string[] fileNames = _fileOrFolderDataArrayOnSvr.Where(
                    d => !d.IsDir).Select(d => d.Name).ToArray();
        long[] fileSizes = _fileOrFolderDataArrayOnSvr.Where(
                    d => !d.IsDir).Select(d => d.FileSize).ToArray();
        _currPathInfoAnswerState = new PathInfoAnswerState(folderNames,
                    fileNames, fileSizes);

        msgSvrClAns = new MsgSvrClPathInfoAnswer(_seqNrPathInfoAns++,
				Settings.SvrClModeAsInt == (int)SvrClMode.SERVERWRITABLE,
				_currPathInfoAnswerState,
                MauiProgram.Settings.BufSizeMoreOrLess);
        sendWhatStr = "path info " +
                (_currPathInfoAnswerState.EndReached ?
                "last part" : $"part {_seqNrPathInfoAns}");
        SetFfInfoStateAndImage(_currPathInfoAnswerState.EndReached ?
                            FfState.CONNECTED : FfState.INTRANSACTION);
        return msgSvrClAns;
    }


    protected string SendWhatStrFromCopyMsgtosend(MsgSvrClBase msgSvrClAns)
    {
        if (msgSvrClAns is MsgSvrClErrorAnswer)
        {
            SetFfInfoStateAndImage(FfState.CONNECTED);
            return $"ERRORMSG ({((MsgSvrClErrorAnswer)msgSvrClAns).GetErrMsgsJoined()})";
        }

        if (msgSvrClAns is MsgSvrClCopyAnswer)
        {
            bool isLastPart = ((MsgSvrClCopyAnswer)msgSvrClAns).IsLastPart;
            SetFfInfoStateAndImage(isLastPart ? FfState.CONNECTED : FfState.INTRANSACTION);
            return "copy info: " +
                (isLastPart ? "last part" :
                $"copy info; file {_copyMgr?.NumFilesOpenedOnSrc} of {_copyMgr?.ClientTotalNumFilesToCopyFromOrTo}");
        }

        //JEEWEE
        //if (msgSvrClAns is MsgSvrClCopyToSvrConfirmation)
        //{
        //    SetFfInfoStateAndImage(FfState.CONNECTED);
        //    return "confirmation";
        //}

        // should not happen:
        SetFfInfoStateAndImage(FfState.CONNECTED)       ;
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
        if (parts.Length < 5)
            return $"Internal error (ipData len {parts.Length})";

        if (MauiProgram.Info.FirstModeIsServer)
        {
            MauiProgram.Info.StrPublicIp = parts[0];
            MauiProgram.Info.SetUdpOrId(parts[1], false);
            MauiProgram.Info.StrPublicIpOtherside = parts[3];
            MauiProgram.Info.SetUdpOrId(parts[4], true);
        }
        else
        {
            MauiProgram.Info.StrPublicIp = parts[3];
            MauiProgram.Info.SetUdpOrId(parts[4], false);
            MauiProgram.Info.StrPublicIpOtherside = parts[0];
            MauiProgram.Info.SetUdpOrId(parts[1], true);
        }
        MauiProgram.Info.StrLocalIPSvr = parts[2];

        return "";
    }


    public static IPEndPoint IPEndPointFromIPAsStrPlusPort(string ipAddress, int port)
    {
        return new IPEndPoint(
                    new IPAddress(ipAddress.Split('.')
                        .Select(p => Convert.ToByte(p))
                        .ToArray()),
                    port);
    }



    /// <summary>
    /// Returns "" if success and errMsg if error
    /// </summary>
    /// <returns></returns>
    protected async Task<string> ConnectServerFromClientAsync()
    {
        try
        {
            bool useLocalIP = MauiProgram.Settings.CommunicModeAsInt == (int)CommunicMode.LOCALIP;

            string ipAddressSvr = useLocalIP ?
                    MauiProgram.Info.StrLocalIPSvr :
                    MauiProgram.Info.StrPublicIpOtherside;
            if (useLocalIP)
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);   // Let the OS pick the local address
                _communicClient = new CommunicWrapper(new UdpClient(ipEndPoint));
                Log("client: localIP: created CommunicWrapper with new udpClient with IPEndPoint" +
                    " (IPAddress.Any, 0): " + ipEndPoint);
            }
            else if (MauiProgram.Settings.CommunicModeAsInt == (int)CommunicMode.CENTRALSVR)
            {
                _communicClient = new CommunicWrapper(null);
                Log("client: via centralsvr: created CommunicWrapper with udpClient null)");
            }
            else          // NATHOLEPUNCHING
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, MauiProgram.Info.UdpPort);
                var udpClient = new UdpClient(
                        new IPEndPoint(IPAddress.Any, MauiProgram.Info.UdpPort));
                _communicClient = new CommunicWrapper(udpClient);
                Log("client: NAT hole punching: created CommunicWrapper with udpClient" +
                    " (IPAddress.Any): " + ipEndPoint);
            }

            if (MauiProgram.Settings.CommunicModeAsInt == (int)CommunicMode.NATHOLEPUNCHING)
            {
                LblInfo2 = "Trying NAT hole punching";
                await DoHolePunchingAsync(_communicClient.UdpWrapper);
            }

            if (MauiProgram.Settings.CommunicModeAsInt != (int)CommunicMode.CENTRALSVR)
            {
                IPEndPoint ipEndPoint = IPEndPointFromIPAsStrPlusPort(ipAddressSvr,
                        MauiProgram.Info.UdpPortOtherside);
                _communicClient.UdpWrapper.ConnectToServerFromClient(ipEndPoint);
                Log("client: ConnectToServerFromClient with server enpoint: " + ipEndPoint);
            }

            //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! LETS SEE HOW TO BEST DISPLAY THINGS
            LblInfo2 = "Trying to connect to server" + (useLocalIP ? " (localIP)" : "");
            MauiProgram.Info.IpSvrThatClientConnectedTo = ipAddressSvr;

            return "";
        }
        catch (Exception exc)
        {
            _communicClient = null;
            return MauiProgram.ExcMsgWithInnerMsgs(exc);
        }
    }



    protected async Task DoHolePunchingAsync(UdpWrapper udpWrapper)
    {
        byte[] dummyMessage = Encoding.UTF8.GetBytes("punch");

        int secondsPunching = 25;       // JEEWEE: SETTING
        int milliSecondsSendDelay = 200;// JEEWEE: SETTING
        CancellationTokenSource cts = new(TimeSpan.FromSeconds(secondsPunching)); // total timeout
        bool received = false;

        // START receiver loop (non-blocking)
        var receiveTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await udpWrapper.ReceiveAsync(1);
                    // If you get a message, consider punching successful
                    received = true;
                    break;
                }
                catch (OperationCanceledException)
                {
                    // expected if timeout passed
                    continue;
                }
            }
        }, cts.Token);

        // START sender loop (non-blocking)
        var sendTask = Task.Run(async () =>
        {
            int loopTimes = secondsPunching * 1000 / milliSecondsSendDelay;
            for (int i = 0; i < loopTimes && !cts.Token.IsCancellationRequested && !received; i++)
            {
                await udpWrapper.SendAsync(dummyMessage, dummyMessage.Length,
                IPEndPointFromIPAsStrPlusPort(
                    MauiProgram.Info.StrPublicIpOtherside,
                    MauiProgram.Info.UdpPortOtherside));
                await Task.Delay(200, cts.Token); // give some space between sends
            }
        }, cts.Token);

        // Wait for success or timeout
        Task result = await Task.WhenAny(receiveTask, Task.Delay(secondsPunching * 1000));
        cts.Cancel();

        if (!received)
        {
            var unregisterTask = Task<string>.Run(() => MauiProgram.PostToCentralServerAsync(
                "UNREGISTER", true));
            SetFfInfoStateAndImage(FfState.UNREGISTERED);
            throw new Exception("Unable to connect with other side; unregistering");
        }
    }

    protected async Task<string> TestStunUdpConnection()
    {
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
        IPEndPoint localUdpEndPoint;
        using (var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0)))
        {
            localUdpEndPoint = ((IPEndPoint)udpClient.Client.LocalEndPoint);
        }

        // 2️ Use STUN to find public IP & port
        // Resolve STUN server hostname to an IP address
        var addresses = await Dns.GetHostAddressesAsync(settings.StunServer);
        var stunServerIP = addresses
            .First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        var stunServerEndPoint = new IPEndPoint(stunServerIP, settings.StunPort);

        // Create StunClient3489 instance
        var stunClient = new StunClient3489(stunServerEndPoint, localUdpEndPoint);

        await stunClient.QueryAsync(); // Perform the STUN request

        MauiProgram.Info.NATType = stunClient.State.NatType.ToString();

        return stunClient.State.PublicEndPoint;
    }




    protected static string GetLocalIP_OLD()
    {
        // 20250309 ChatGPT composed me this, but later on (20250630) I found that there multiple
        // addresses result, of which FirstOrDefault() is an arbitrary one. Therefor, many times
        // a Local IP connection didn't work. Use GetLocalIP() .
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up) // Only active network interfaces
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
            .Select(a => a.Address.ToString())
            .FirstOrDefault() ?? "";
    }


    protected static string GetLocalIP()
    {
        // 20250630 ChatGPT composed me this:
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect("192.168.1.1", 65530); // must be a local IPdoesn't actually send packets
        if (socket.LocalEndPoint is IPEndPoint endPoint)
        {
            return endPoint.Address.ToString();
        }
        return "";
    }



    /// <summary>
    /// Returns true if msgSvrCl is MsgSvrClDisconnInfo, else false
    /// If true, displays alert
    /// </summary>
    /// <param name="msgSvrCl"></param>
    /// <returns></returns>
    protected async Task<bool> OthersideIsDisconnected_msgbox_Async(MsgSvrClBase msgSvrCl)
    {
        if (!(msgSvrCl is MsgSvrClDisconnInfo))
            return false;

        string otherIsSvrCl = MauiProgram.Settings.ModeIsServer ? "Client" : "Server";

        // JWdP 20250928 I wanted to reset the app without shutting down, to a state where user can reconnect,
        // but I´m getting too many difficult exceptions when trying to reastablish communication when other side
        // restarts, like something like 'requires a full address'. Better shutdown alltogether.
        string logLine =
            $"{otherIsSvrCl} has disconnected: shutting down this app";
        Log(logLine);
        await Shell.Current.DisplayAlert("Disconnected", logLine, "OK");
        var unregisterTask = Task<string>.Run(() => MauiProgram.PostToCentralServerAsync(
            "UNREGISTER", true));
        SetFfInfoStateAndImage(FfState.UNREGISTERED);

        IsBusy = false;
        OnPropertyChanged();
        MauiProgram.OnCloseThingsTotally();     // this calls Info.MainPageVwModel.OnCloseThings()
        Application.Current.Quit();
        return true;
    }


    /// <summary>
    /// returns value of found propName, "" if value empty or prop not found
    /// </summary>
    /// <param name="strJson"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    protected static string GetJsonProp(string strJson, string propName)
    {
        string search = $"\"{propName}\":";
        int idx = strJson.IndexOf(search);
        if (idx == -1)
            return "";
        idx += search.Length;
        if (idx >= strJson.Length)
            return "";
        if (strJson[idx] == '"')
        {
            idx++;
            int idx2 = strJson.IndexOf("\"", idx);
            if (idx2 == -1)
                return "";
            return strJson.Substring(idx, idx2 - idx);
        }
        int idx3 = strJson.IndexOf(",", idx);
        if (idx3 == -1)
            idx3 = strJson.IndexOf("}", idx);
        if (idx3 == -1)
            idx3 = strJson.Length;
        return strJson.Substring(idx, idx3 - idx).Trim();
    }



    protected class UdpWrapper : IDisposable
    {
        protected UdpClient _udpClient;       // also for server
        protected IPEndPoint _ipEndPointClient = null;
        public IPEndPoint IPEndPointClient { get => _ipEndPointClient; }

        public UdpWrapper(UdpClient udpClient)
        {
            _udpClient = udpClient;
        }


        public void ConnectToServerFromClient(IPEndPoint ipEndPoint)
        {
            _udpClient.Connect(ipEndPoint);
        }


        /// <summary>
        /// Must be done only the first time that server recieves data from client.
        /// Also after swap: must not be overwritten
        /// </summary>
        /// <param name="ipEndPointClient"></param>
        public void SetClientRemoteEnd(IPEndPoint ipEndPointClient)
        {
            _ipEndPointClient = ipEndPointClient;
        }


        /// <summary>
        /// ReceiveAsync
        /// </summary>
        /// <param name="timeOutSeconds">-1 means no timeout</param>
        /// <returns></returns>
        public async Task<UdpReceiveResult> ReceiveAsync(int timeOutSeconds = -1)
        {
            UdpReceiveResult result = -1 == timeOutSeconds ?
                    await _udpClient.ReceiveAsync()
                    :
                    await _udpClient.ReceiveAsync(new CancellationTokenSource(
                        timeOutSeconds * 1000).Token);
            return result;
        }


        public async Task<int> SendAsync(byte[] bytes, int numBytesToSend, IPEndPoint ipEndPointOverride = null)
        {
            IPEndPoint ipEndPoint = ipEndPointOverride ?? _ipEndPointClient;
            if (null != ipEndPoint)
                return await _udpClient.SendAsync(bytes, numBytesToSend,
                        ipEndPoint);

            return await _udpClient.SendAsync(bytes, numBytesToSend);
        }


        public int Send(byte[] bytes, int numBytesToSend, IPEndPoint ipEndPointOverride = null)
        {
            IPEndPoint ipEndPoint = ipEndPointOverride ?? _ipEndPointClient;
            if (null != ipEndPoint)
                return _udpClient.Send(bytes, numBytesToSend,
                        ipEndPoint);

            return _udpClient.Send(bytes, numBytesToSend);
        }


        public void Dispose()
        {
            _udpClient.Dispose();
        }
    }



    protected class CommunicWrapper : IDisposable
    {
        public UdpWrapper UdpWrapper { get; protected set; } = null;
        public IPEndPoint LastReceivedRemoteEndPoint { get; protected set; } = null;
        protected string _csvr_downloadUrl = $"https://www.cadh5.com/farfiles/dwnld.php";
        protected string _csvr_uploadUrl = $"https://www.cadh5.com/farfiles/upld.php";
        protected int _rcvNr = 1;
        protected int _sndNr = 1;

        public CommunicWrapper(UdpClient udpClientOrNull)
        {
            if (null != udpClientOrNull)
                UdpWrapper = new UdpWrapper(udpClientOrNull);
        }

        public void ConnectToServerFromClient(IPEndPoint ipEndPoint)
        {
            if (null != UdpWrapper)
                UdpWrapper.ConnectToServerFromClient(ipEndPoint);
        }


        public void SetClientRemoteEnd(IPEndPoint ipEndPointClient)
        {
            if (null != UdpWrapper)
                UdpWrapper.SetClientRemoteEnd(ipEndPointClient);
        }


        /// <summary>
        /// ReceiveBytesAsync
        /// </summary>
        /// <param name="timeOutSeconds">-1 means no timeout</param>
        /// <returns></returns>
        public async Task<byte[]> ReceiveBytesAsync(int pollSleepSeconds, int timeOutSeconds = -1)
        {
            string svrCl = MauiProgram.Settings.ModeIsServer ? "server" : "client";
            if (null != UdpWrapper)
            {
                UdpReceiveResult result = await UdpWrapper.ReceiveAsync(timeOutSeconds);
                LastReceivedRemoteEndPoint = result.RemoteEndPoint;
                MauiProgram.Log.LogLine($"{svrCl}: ReceiveBytesAsync: received {result.Buffer.Length} byte(s)");
                return result.Buffer;
            }

            //JEEWEE: SECONDS: NEW SETTING?!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // no udp communication: poll central server for file
            string pollingFileName = CsvrComposeFileName(
                    MauiProgram.Info.StrPublicIp,
                    MauiProgram.Info.UdpPort, MauiProgram.Info.IdInsteadOfUdp,
                    _rcvNr);
            string urlRcvFile = _csvr_downloadUrl + "?filenameext=" + pollingFileName;

            MauiProgram.Log.LogLine($"{svrCl}: downloading: {urlRcvFile}");

            using (var client = new HttpClient())
            {
                CancellationTokenSource cts = new(TimeSpan.FromSeconds(MauiProgram.Settings.TimeoutSecsClient)); // total timeout
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        byte[] retBytes = await client.GetByteArrayAsync(urlRcvFile);
                        if (retBytes != null && retBytes.Length > 0)
                        {
                            _rcvNr++;
                            return retBytes;
                        }
                    }
                    catch (Exception exc)
                    {
                        MauiProgram.Log.LogLine($"{svrCl}, ReceiveBytesAsync: polling at {pollSleepSeconds} seconds, central server: " +
                                MauiProgram.ExcMsgWithInnerMsgs(exc));
                    }
                    await Task.Delay(pollSleepSeconds * 1000);
                }
            }

            return new byte[0];
        }


        public async Task<int> SendBytesAsync(byte[] bytes, int numBytesToSend, IPEndPoint ipEndPointOverride = null)
        {
            if (null != UdpWrapper)
            {
                return await UdpWrapper.SendAsync(bytes, numBytesToSend, ipEndPointOverride);
            }

            // no udp communication: upload to central server as a file
            var content = new MultipartFormDataContent();
            using (var client = new HttpClient())
            {
                // Fake the file using ByteArrayContent
                var byteContent = new ByteArrayContent(bytes, 0, numBytesToSend);
                byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                            "application/octet-stream");
                string fileNameExt = CsvrComposeFileName(MauiProgram.Info.StrPublicIpOtherside,
                            MauiProgram.Info.UdpPortOtherside, MauiProgram.Info.IdInsteadOfUdpOtherSide,
                            _sndNr);
                content.Add(byteContent, "filesToUpload[]", fileNameExt);

                string svrCl = MauiProgram.Settings.ModeIsServer ? "server" : "client";
                MauiProgram.Log.LogLine($"{svrCl}: uploading: {fileNameExt}");

                HttpResponseMessage response = await client.PostAsync(_csvr_uploadUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Errorstatus {response.StatusCode} while sending to central server: " +
                        responseText);
                }

                _sndNr++;
            }

            return numBytesToSend;
        }



        public void Dispose()
        {
            if (null != UdpWrapper)
                UdpWrapper.Dispose();
        }


        public static string CsvrComposeFileName(string strIp, int udpPort,
                                string idInsteadOfUdp, int seqNr)
        {
            string udpOrId = udpPort != 0 ? udpPort.ToString() : idInsteadOfUdp;
            string[] parts = strIp.Split('.');
            if (parts.Length != 4)
                throw new Exception($"PROGRAMMERS: CsvrComposeFileName: invalid ip '{strIp}'");
            return String.Join('-', parts) + $"_{udpOrId}_{seqNr}.bin";
        }
    }
}
