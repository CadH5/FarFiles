//JEEWEE
//using Android.Widget;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;

using FarFiles.Services;
using Microsoft.Maui.Controls.Internals;
using System.Threading;

namespace FarFiles.ViewModel;

public partial class ClientViewModel : BaseViewModel
{
    public ClientPage ContentPageRef;
    public ObservableCollection<Model.FileOrFolderData> FileOrFolderColl { get; } = new();
    FileDataService fileDataService;

    // xaml cannot bind to MauiProgram.Settings directly.
    // And for Idx0isOverwr1isSkip an extra measure is necessary:
    public Settings Settings { get; protected set; } = MauiProgram.Settings;

    protected bool _abortCopy = false;
    protected bool _isCopying = false;
    public bool IsCopying
    {
        get => _isCopying;
        set
        {
            _isCopying = value; OnPropertyChanged();
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

    public ClientViewModel(FileDataService fileDataService, IConnectivity connectivity)
    {
        Title = "Far Away Files Access";
        this.fileDataService = fileDataService;

        UpdateCollView();
    }


    protected void UpdateCollView()
    {
        FileOrFolderColl.Clear();

        if (MauiProgram.Info.SvrPathParts.Count > 0)
            FileOrFolderColl.Add(new FileOrFolderData("..", true, 0));

        foreach (FileOrFolderData fo in MauiProgram.Info.CurrSvrFolders)
            FileOrFolderColl.Add(fo);
        foreach (FileOrFolderData fi in MauiProgram.Info.CurrSvrFiles)
            FileOrFolderColl.Add(fi);

        //JEEWEE
        ContentPageRef?.DoWeird(FileOrFolderColl);        // otherwise sometimes items in new contents seem selected
    }


    public string TxtLocalRoot
    {
        get => $"Local path: {MauiProgram.Settings.FullPathRoot}";
    }

    public string TxtSvrPath
    {
        get => $"Sub path on server: '{String.Join('/', MauiProgram.Info.SvrPathParts)}'";
    }

    [RelayCommand]
    async Task ClrAll()
    {
        ContentPageRef.ClrAll();
    }

    [RelayCommand]
    async Task GotoDirAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            FileOrFolderData[] selecteds = ContentPageRef.GetSelecteds();
            if (selecteds.Length != 1 || ! selecteds.First().IsDir)      // button should be disabled
                throw new Exception(
                    $"PROGRAMMERS: GotoDirAsync: num selecteds ({selecteds.Length}) not 1" +
                    " or selected is not dir");

            FileOrFolderData gotoDir = selecteds.First();

            int numSvrPartsGoto = MauiProgram.Info.SvrPathParts.Count;
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
            OnPropertyChanged("TxtSvrPath");

            await MauiProgram.Info.MainPageVwModel.SndFromClientRecievePathInfo_msgbxs_Async();
            UpdateCollView();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to goto dir: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }



    [RelayCommand]
    async Task CopyAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            IsCopying = true;
            _abortCopy = false;
            FileOrFolderData[] selecteds = ContentPageRef.GetSelecteds();
            LblFileNofN = "";
            LblByteNofN = "";

            bool accepted = await Shell.Current.DisplayAlert("Start copy?",
                $"Start copying selected {selecteds.Length} file(s) and/or folder(s) with content(s), " +
                (0 == MauiProgram.Settings.Idx0isOverwr1isSkip ? "overwriting" : "skipping") +
                " existing files?",
                "OK", "Cancel");
            if (!accepted)
                return;

            await MauiProgram.Info.MainPageVwModel.CopyFromSvr_msgbxs_Async(selecteds,
                        FuncCopyGetAbortSetLbls);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get filesdata: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsCopying = false;
            LblFileNofN = "";
            LblByteNofN = "";
        }
    }


    [RelayCommand]
    async Task AbortCopy()
    {
        // IsBusy will be true for this button

        bool accepted = await Shell.Current.DisplayAlert("Abort copy?",
            $"Abort copy operation?",
            "OK", "Cancel");
        if (!accepted)
            return;

        _abortCopy = true;
    }


    protected bool FuncCopyGetAbortSetLbls(int fileN, int filesTotal,
                long byteN, long bytesTotal)
    {
        if (_abortCopy)
        {
            LblFileNofN = $"Aborted by user";
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
}
