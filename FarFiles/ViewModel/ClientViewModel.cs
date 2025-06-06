﻿using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;

using FarFiles.Services;
using Microsoft.Maui.Controls.Internals;
using System.Threading;

namespace FarFiles.ViewModel;

public partial class ClientViewModel : BaseViewModel
{
    public ClientPage ContentPageRef;
    public ObservableCollection<FfCollViewItem> FfColl { get; } = new();
    FileDataService fileDataService;

    // xaml cannot bind to MauiProgram.Settings directly.
    // And for Idx0isOverwr1isSkip an extra measure is necessary:
    public Settings Settings { get; protected set; } = MauiProgram.Settings;

    protected CpClientToFromMode _copyToFromSvrMode = CpClientToFromMode.CLIENTFROMSVR;
    public CpClientToFromMode CopyToFromSvrMode
    {
        get => _copyToFromSvrMode;
        set
        {
            _copyToFromSvrMode = value; OnPropertyChanged();
        }
    }


    public bool IsSvrWritable { get => MauiProgram.Info.IsSvrWritableReportedToClient; }

    public string TxtBtnCopyToFromSvr
    {
        // the buttontext must be precisely the oposite of the current state:
        get => _copyToFromSvrMode == CpClientToFromMode.CLIENTFROMSVR ?
                "copy TO server" : "copy from server";
    }



    protected bool _moreButtonsMode = false;
    public bool MoreButtonsMode
    {
        get => _moreButtonsMode;
        set
        {
            _moreButtonsMode = value;
            IsBusy = value;         // to disable/enable CollectionView
            OnPropertyChanged();
            ContentPageRef.SetValuesForUpdpgDoUpd(IsBusy, MoreButtonsMode);
        }
    }

    public bool IsBusyPlus
    {
        get => IsBusy;
        set
        {
            IsBusy = value;
            OnPropertyChanged();
            ContentPageRef.SetValuesForUpdpgDoUpd(IsBusy, MoreButtonsMode);
        }
    }

    protected bool _abortProgress = false;
    protected bool _isProgressing = false;
    public bool IsProgressing
    {
        get => _isProgressing;
        set
        {
            _isProgressing = value; OnPropertyChanged();
        }
    }

    protected string _lblFileNofN = "";
    public string LblFileNofN
    {
        get => _lblFileNofN;
        set
        {
            _lblFileNofN = value; OnPropertyChanged();
        }
    }

    protected string _lblByteNofN = "";
    public string LblByteNofN
    {
        get => _lblByteNofN;
        set
        {
            _lblByteNofN = value; OnPropertyChanged();
        }
    }


    protected string _txtSelectFltr = "";
    public string TxtSelectFltr
    {
        get => _txtSelectFltr;
        set
        {
            _txtSelectFltr = value; OnPropertyChanged();
        }
    }

    public ClientViewModel(FileDataService fileDataService, IConnectivity connectivity)
    {
        Title = "Far Away Files Access";
        this.fileDataService = fileDataService;

        UpdateCollView();
    }


    protected void UpdateCollView()
    {
        FfColl.Clear();

        if (MauiProgram.Info.SvrPathParts.Count > 0)
            FfColl.Add(new FfCollViewItem(new FileOrFolderData("..", true, 0)));

        foreach (FileOrFolderData fo in MauiProgram.Info.CurrSvrFolders)
            FfColl.Add(new FfCollViewItem(fo));
        foreach (FileOrFolderData fi in MauiProgram.Info.CurrSvrFiles)
            FfColl.Add(new FfCollViewItem(fi));

        //JEEWEE
        //ContentPageRef?.DoWeird(FfColl);        // otherwise sometimes items in new contents seem selected
    }


    public string TxtLocalRoot
    {
        get => $"Local path: {MauiProgram.Settings.FullPathRoot}";
    }

    public string TxtSvrPath
    {
        get
        {
            string writable = MauiProgram.Info.IsSvrWritableReportedToClient ?
                " (writable)" : "";
            return $"Sub path on server{writable}: '{String.Join('/', MauiProgram.Info.SvrPathParts)}'";
        }
    }

    [RelayCommand]
    void ClrAll()
    {
        ContentPageRef.ClrAll(FfColl);
    }


    [RelayCommand]
    void SelectAll()
    {
        ContentPageRef.SelectAll(FfColl);
        MoreButtonsMode = false;
    }

    [RelayCommand]
    void SelectFltr()
    {
        ContentPageRef.SelectFltr(FfColl, TxtSelectFltr);
        MoreButtonsMode = false;
    }


    [RelayCommand]
    async Task GotoDirAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusyPlus = true;
            IsProgressing = true;
            _abortProgress = false;

            var savSvrPathParts = new List<string>();
            savSvrPathParts.AddRange(MauiProgram.Info.SvrPathParts);

            FfCollViewItem[] selecteds = ContentPageRef.GetSelecteds();
            if (selecteds.Length != 1 || ! selecteds.First().FfData.IsDir)      // button should be disabled
                throw new Exception(
                    $"PROGRAMMERS: GotoDirAsync: num selecteds ({selecteds.Length}) not 1" +
                    " or selected is not dir");

            FileOrFolderData gotoDir = selecteds.First().FfData;

            if (gotoDir.Name == "..")
            {
                if (MauiProgram.Info.SvrPathParts.Count > 0)    // should be
                    MauiProgram.Info.SvrPathParts.RemoveAt(
                                MauiProgram.Info.SvrPathParts.Count - 1);
            }
            else
            {
                MauiProgram.Info.SvrPathParts.Add(gotoDir.Name);
            }

            Exception excSendRcv = null;
            try
            {
                await MauiProgram.Info.MainPageVwModel.SndFromClientRecievePathInfo_msgbxs_Async(
                            FuncPathInfoGetAbortSetLbls);
            }
            catch (Exception exc)
            {
                excSendRcv = exc;
            }

            if (_abortProgress || null != excSendRcv)
            {
                MauiProgram.Info.SvrPathParts.Clear();
                MauiProgram.Info.SvrPathParts.AddRange(savSvrPathParts);
            }
            OnPropertyChanged("TxtSvrPath");

            if (null != excSendRcv)
                throw excSendRcv;
        }
        catch (Exception exc)
        {
            Debug.WriteLine($"Unable to goto dir: {exc.Message}");
            await Shell.Current.DisplayAlert("Error!", exc.Message, "OK");
        }
        finally
        {
            IsBusyPlus = false;
            IsProgressing = false;
            UpdateCollView();
        }
    }



    [RelayCommand]
    async Task CopyAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusyPlus = true;
            IsProgressing = true;
            _abortProgress = false;

            ContentPageRef.ClrDotDotAt0();
            FfCollViewItem[] selecteds = ContentPageRef.GetSelecteds();
            if (selecteds.Length == 0)      // possible if they only had selected ".."
                return;

            LblFileNofN = "waiting for server ...";
            LblByteNofN = "";

            bool accepted = await Shell.Current.DisplayAlert("Start copy?",
                $"Start copying selected {selecteds.Length} file(s) and/or folder(s) with content(s), " +
                (0 == MauiProgram.Settings.Idx0isOverwr1isSkip ? "overwriting" : "skipping") +
                " existing files?",
                "OK", "Cancel");
            if (!accepted)
                return;

            await MauiProgram.Info.MainPageVwModel.CopyFromSvr_msgbxs_Async(
                        selecteds.Select(i => i.FfData).ToArray(),
                        FuncCopyGetAbortSetLbls);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get filesdata: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusyPlus = false;
            IsProgressing = false;
            LblFileNofN = "";
            LblByteNofN = "";
        }
    }


    [RelayCommand]
    void MoreButtons()
    {
        MoreButtonsMode = ! MoreButtonsMode;
    }



    [RelayCommand]
    async Task AbortProgress()
    {
        // IsBusy will be true for this button

        bool accepted = await Shell.Current.DisplayAlert("Abort?",
            $"Abort operation?",
            "OK", "Cancel");
        if (!accepted)
            return;

        _abortProgress = true;
    }


    protected bool FuncCopyGetAbortSetLbls(int fileN, int filesTotal,
                long byteN, long bytesTotal)
    {
        if (_abortProgress)
        {
            LblFileNofN = $"Aborted by user";
            LblByteNofN = "";
            return true;
        }

        LblFileNofN = $"file {fileN} of {filesTotal} ...";
        LblByteNofN = bytesTotal < 100000 ? "" :
                $"byte {BytesStr(byteN)} of {BytesStr(bytesTotal)} ...";
        return false;
    }

    protected string BytesStr(long numBytes)
    {
        string retStr = numBytes.ToString();
        for (int i=retStr.Length - 3; i > 0; i -= 3)
        {
            retStr = retStr.Substring(0, i) + "." + retStr.Substring(i);
        }

        return retStr;
    }

    protected bool FuncPathInfoGetAbortSetLbls(int seqNr)
    {
        if (_abortProgress)
        {
            LblFileNofN = "";
            LblByteNofN = "";
            return true;
        }

        LblFileNofN = $"android on server busy ({seqNr}) ...";
        LblByteNofN = "";
        return false;
    }

}


public class FfCollViewItem : INotifyPropertyChanged
{
    public Model.FileOrFolderData FfData { get; set; }

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected != value)
            {
                isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public FfCollViewItem(FileOrFolderData fData)
    {
        FfData = fData;
        IsSelected = false;
    }

}
