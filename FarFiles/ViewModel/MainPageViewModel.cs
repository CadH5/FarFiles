﻿using CommunityToolkit.Maui.Alerts;
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
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace FarFiles.ViewModel;

public partial class MainPageViewModel : BaseViewModel
{
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
                OnPropertyChanged(nameof(IsChkLocalIPVisible));
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
                OnPropertyChanged(nameof(IsChkLocalIPVisible));
            }
        }
    }

    public bool IsBtnBackToFilesVisible
    {
        get => ! IsBtnConnectVisible;
    }

    public bool IsChkLocalIPVisible
    {
        get => IsBtnConnectVisible && ! MauiProgram.Settings.ModeIsServer;
    }


    public bool UseSvrLocalIPClient
    {
        get => Settings.UseSvrLocalIPClient;
        set
        {
            if (Settings.UseSvrLocalIPClient != value)
            {
                Settings.UseSvrLocalIPClient = value;
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
                //JEEWEE
                //{
                //    throw new Exception($"FolderPicker not successful or cancelled");
                //}

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
        if (String.IsNullOrEmpty(MauiProgram.Settings.ConnectKey))
        {
            await Shell.Current.DisplayAlert("Info",
                "Connect: Please enter connect key first", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            string msg = "";

            int udpSvrPort = 0;
            if (MauiProgram.Settings.ModeIsServer)
            {
                // server: get udp port from Stun server
                var udpIEndPoint = await GetUdpSvrIEndPointFromStun(Settings);
                if (null == udpIEndPoint)
                    throw new Exception("Error getting data from Stun Server");
                udpSvrPort = udpIEndPoint.Port;
                if (udpSvrPort <= 0)
                    throw new Exception("Wrong data from Stun Server");
            }

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

            if (MauiProgram.Settings.ModeIsServer)
            {
                // server: do conversation: in loop: listen for client msgs and response
                LblInfo1 = $"Connected; listening for client contact ...";
                using (var udpServer = new UdpClient(udpSvrPort))
                {
                    while (true)
                    {
                        await ListenMsgAndSendMsgOnSvrAsync(udpServer);
                    }
                }
            }
            else
            {
                // client: connect to server
                errMsg = ConnectServerFromClient();
                if ("" != errMsg)
                {
                    throw new Exception(errMsg);
                }

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
                    DisconnectAndResetOnClient();
                    LblInfo2 = "retrieving server path info failed";
                }
            }
        }
        catch (Exception exc)
        {
            await Shell.Current.DisplayAlert("Error",
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
        
        msgSvrClToSend = new MsgSvrClPathInfoRequest(MauiProgram.Info.SvrPathParts);

        var lisFolders = new List<string>();
        var lisFiles = new List<string>();
        var lisSizes = new List<long>();

        bool abort = false;
        int sleepMilliSecs = 1000;

        LblInfo1 = "sending path info request to server ...";
        while (true)
        {
            int seqNr;
            Log($"client: sending to server: {msgSvrClToSend.GetType()}");
            byte[] byRecieved = await SndFromClientRecieve_msgbxs_Async(
                                msgSvrClToSend.Bytes);
            LblInfo1 = "";
            if (byRecieved.Length == 0)
            {
                DisconnectAndResetOnClient();
                return false;
            }
            LblInfo2 = "receiving path info from server ...";

            MsgSvrClBase msgSvrClAnswer = MsgSvrClBase.CreateFromBytes(byRecieved);
            Log($"client: received from server: {msgSvrClAnswer.GetType()}");
            if (msgSvrClAnswer is MsgSvrClAbortedConfirmation)
                break;

            if (msgSvrClAnswer is MsgSvrClPathInfoAndroidBusy)
            {
                ((MsgSvrClPathInfoAndroidBusy)msgSvrClAnswer).GetSeqnr(out seqNr);
                Thread.Sleep(sleepMilliSecs);
                sleepMilliSecs = Math.Min(3000, sleepMilliSecs + 1000);
                LblInfo1 = "sending still busy inquiry to server ...";
                msgSvrClToSend = new MsgSvrClPathInfoAndroidStillBusyInq();
            }
            else
            {
                msgSvrClAnswer.CheckExpectedTypeMaybeThrow(typeof(MsgSvrClPathInfoAnswer));
                Log($"client: received bytes: {byRecieved.Length}, MsgSvrClPathInfoAnswer");

                ((MsgSvrClPathInfoAnswer)msgSvrClAnswer).GetSeqnrAndIswrAndIslastAndFolderAndFileNamesAndSizes(
                        out seqNr, out bool isSvrWritable, out bool isLast, out string[] folderNames, out string[] fileNames, out long[] fileSizes);
                lisFolders.AddRange(folderNames);
                lisFiles.AddRange(fileNames);
                lisSizes.AddRange(fileSizes);

                MauiProgram.Info.IsSvrWritableReportedToClient = isSvrWritable;

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


    protected void DisconnectAndResetOnClient()
    {
        MauiProgram.Info.DisconnectOnClient();
        IsBtnConnectVisible = true;
        OnPropertyChanged();
    }


    public async Task CopyFromOrToSvrOnClient_msgbxs_Async(
            CpClientToFromMode copyToFromSvrMode,
            FileOrFolderData[] selecteds,
            Func<int, int, long, long, bool> funcCopyGetAbortSetLbls = null)
    {
        IEnumerable<string> selectedDirs = selecteds.Where(f => f.IsDir).Select(f => f.Name);
        IEnumerable<string> selectedFiles = selecteds.Where(f => !f.IsDir).Select(f => f.Name);

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
                byte[] byRecieved = await SndFromClientRecieve_msgbxs_Async(
                                    msgSvrCl.Bytes);
                if (byRecieved.Length == 0)
                    //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! make CopyMgr abort
                    return;

                MsgSvrClBase msgSvrClRecieved = MsgSvrClBase.CreateFromBytes(byRecieved);
                if (msgSvrClRecieved is MsgSvrClAbortedConfirmation)
                    break;

                Type expectedType = copyToFromSvrMode == CpClientToFromMode.CLIENTFROMSVR ?
                    typeof(MsgSvrClCopyAnswer) : typeof(MsgSvrClCopyToSvrConfirmation);
                msgSvrClRecieved.CheckExpectedTypeMaybeThrow(expectedType);
                Log($"client: received bytes: {byRecieved.Length}, {expectedType}");

                if (copyToFromSvrMode == CpClientToFromMode.CLIENTFROMSVR)
                {
                    if (copyMgr.CreateOnDestFromNextPart((MsgSvrClCopyAnswer)msgSvrClRecieved,
                                                    funcCopyGetAbortSetLbls))
                    {
                        // ready
                        copyMgr.LogErrMsgsIfAny("client ErrMsgs:");
                        nums = copyMgr.Nums;
                        numErrMsgsClient = copyMgr.ErrMsgs.Count;
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
                        ((MsgSvrClCopyToSvrConfirmation)msgSvrClRecieved).GetNums(
                            out nums, out numErrMsgsSvr);
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
                (numErrMsgsSvr > 0 ? $"ERRORS on server: {numErrMsgsSvr}{nl}" : "") +
                (numErrMsgsClient > 0 ? $"ERRORS on client: {numErrMsgsClient}{nl}" : "") +
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



    /// <summary>
    /// Sends bytes to server and receives bytes. If exception, displays alert and returns [0] bytes
    /// </summary>
    /// <param name="sendBytes"></param>
    /// <returns></returns>
    protected async Task<byte[]> SndFromClientRecieve_msgbxs_Async(byte[] sendBytes)
    {
        try
        {
            int iResult = await _udpClient.SendAsync(sendBytes, sendBytes.Length);

            Log($"client: sent to server: msg {++_numSendMsg}, {iResult} bytes, waiting for server...");

            UdpReceiveResult response = await _udpClient.ReceiveAsync(
                                new CancellationTokenSource(
                                    MauiProgram.Settings.TimeoutSecsClient * 1000).Token);

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



    protected async Task ListenMsgAndSendMsgOnSvrAsync(UdpClient udpServer)
    {
        MsgSvrClBase msgSvrCl = null;

        try
        {
            UdpReceiveResult received = await udpServer.ReceiveAsync();
            msgSvrCl = MsgSvrClBase.CreateFromBytes(received.Buffer);
            
            MsgSvrClBase msgSvrClAns = null;

            //JEEWEE: NEXT IS TOO COMPLICATED FOR FUTURE CHANGES
            //// we should close _reader if for any reason another message comes in than as expected
            //if (null != _copyMgr && !(msgSvrCl is MsgSvrClCopyNextpartRequest))
            //{
            //    _copyMgr.LogErrMsgsIfAny("server ErrMsgs:");
            //    _copyMgr.Dispose();
            //    _copyMgr = null;
            //}

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
                // on Windows there is no performance trouble
                GetFileOrFolderDataArray(svrSubParts);
                msgSvrClAns = HandleFileOrFolderDataArrayOnSvr(out sendWhatStr);
#endif

            }
            else if (msgSvrCl is MsgSvrClPathInfoAndroidStillBusyInq)
            {
                LblInfo1 = "received: inquiry 'is Android still busy'";
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
                    msgSvrClAns = new MsgSvrClErrorAnswer(
                        $"Server: wrong request last {msgSvrCl.GetType()}, no active path info answer state");
                    sendWhatStr = "ERRORMSG (wrong request next part path info)";
                }
                else
                {
                    msgSvrClAns = new MsgSvrClPathInfoAnswer(_seqNrPathInfoAns++,
                            Settings.SvrClModeAsInt == (int)SvrClMode.SERVERWRITABLE,
                            _currPathInfoAnswerState,
                            MauiProgram.Settings.BufSizeMoreOrLess);
                    sendWhatStr = "path info part " + _seqNrPathInfoAns;
                }
            }
            else if (msgSvrCl is MsgSvrClCopyRequest)
            {
                // Start of a copy from svr to client operation
                LblInfo1 = "received: copy request";
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
                }
                else
                {
                    LblInfo1 = "received: next part copy request";
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
                    LblInfo1 = "received: start data copy TO server";
                }
                else
                {
                    LblInfo1 = "received: next data copy TO server";
                }

                var msgSvrClCpPart = (MsgSvrClCopyToSvrPart)msgSvrCl;
                bool isLast = _copyMgr.CreateOnDestFromNextPart(msgSvrClCpPart);
                msgSvrClAns = new MsgSvrClCopyToSvrConfirmation(
                    _copyMgr.Nums, _copyMgr.ErrMsgs.Count);
                if (isLast)
                {
                    _copyMgr.LogErrMsgsIfAny("server ErrMsgs:");
                    _copyMgr.Dispose();
                    _copyMgr = null;
                }
                sendWhatStr = SendWhatStrFromCopyMsgtosend(msgSvrClAns);
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
                msgSvrClAns = new MsgSvrClErrorAnswer(
                    $"Server: received unexpected message type {msgSvrCl.GetType()}");
                sendWhatStr = $"ERRORMSG (wrong request {msgSvrCl.GetType()})";
            }

            //5️ Respond to client(hole punching)

            LblInfo2 = $"sending {sendWhatStr} ...";
            Log($"server: going to send bytes: {msgSvrClAns.Bytes.Length}, {msgSvrClAns.GetType()}");
            await udpServer.SendAsync(msgSvrClAns.Bytes, msgSvrClAns.Bytes.Length,
                        received.RemoteEndPoint);
            LblInfo2 = $"sent: {sendWhatStr}";

            MauiProgram.Info.NumAnswersSent++;
        }
        catch (Exception exc)
        {
            Log($"server: EXCEPTION: ListenMsgAndSendMsgOnSvrAsync: " +
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
					Settings.SvrClModeAsInt == (int)SvrClMode.SERVERWRITABLE,
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
                $"copy info; file {_copyMgr?.NumFilesOpenedOnSrc} of {_copyMgr?.ClientTotalNumFilesToCopyFromOrTo}");

        if (msgSvrClAns is MsgSvrClCopyToSvrConfirmation)
            return "confirmation";

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


    /// <summary>
    /// Returns "" if success and errMsg if error
    /// </summary>
    /// <returns></returns>
    protected string ConnectServerFromClient()
    {
        try
        {
            string ipAddress = UseSvrLocalIPClient ?
                    MauiProgram.Info.LocalIpSvrRegistered :
                    MauiProgram.Info.PublicIpSvrRegistered;
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0)); // Let the OS pick the local address
            _udpClient.Connect(new IPEndPoint(
                    new IPAddress(ipAddress.Split('.')
                        .Select(p => Convert.ToByte(p))
                        .ToArray()),
                    Convert.ToInt32(
                        MauiProgram.Info.PublicUdpPortSvrRegistered)));
            LblInfo2 = "Trying to connect to server" + (UseSvrLocalIPClient ? " (localIP)" : "");
            MauiProgram.Info.IpSvrThatClientConnectedTo = ipAddress;
            return "";
        }
        catch (Exception exc)
        {
            _udpClient = null;
            return MauiProgram.ExcMsgWithInnerMsgs(exc);
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
            int localUdpPort = localUdpEndPoint.Port;
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

        return stunClient.State.PublicEndPoint;
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
