using STUN;

namespace FarFiles.ViewModel;

public partial class AdvancedViewModel : BaseViewModel
{
    string nl = Environment.NewLine;
    public string Info
    {
        get =>
            (MauiProgram.Info.UdpSvrPort <= 0 ? "" :
                $"UdpSvrPort: {MauiProgram.Info.UdpSvrPort}{nl}") +
            $"PublicIpSvrRegistered: {MauiProgram.Info.PublicIpSvrRegistered}{nl}" +
            $"PublicUdpPortSvrRegistered: {MauiProgram.Info.PublicUdpPortSvrRegistered}{nl}" +
            $"LocalIpSvrRegistered: {MauiProgram.Info.LocalIpSvrRegistered}{nl}" +
            $"IpSvrThatClientConnectedTo: {MauiProgram.Info.IpSvrThatClientConnectedTo}{nl}" +
            $"Connected: {MauiProgram.Info.Connected}";
    }

    public Settings Settings { get; protected set; } = MauiProgram.Settings;

    public bool SettingsSvrVis { get => MauiProgram.Settings.Idx0isSvr1isCl == 0; }
    public bool SettingsClientVis { get => MauiProgram.Settings.Idx0isSvr1isCl == 1; }

    public string StunServer
    {
        get => Settings.StunServer;
        set
        {
            if (Settings.StunServer != value)
            {
                Settings.StunServer = value;
                OnPropertyChanged();
            }
        }
    }

    public int StunPort
    {
        get => Settings.StunPort;
        set
        {
            if (Settings.StunPort != value)
            {
                Settings.StunPort = value;
                OnPropertyChanged();
            }
        }
    }

    public int TimeoutSecsClient
    {
        get => Settings.TimeoutSecsClient;
        set
        {
            if (Settings.TimeoutSecsClient != value)
            {
                Settings.TimeoutSecsClient = value;
                OnPropertyChanged();
            }
        }
    }

    public int BufSizeMoreOrLess
    {
        get => Settings.BufSizeMoreOrLess;
        set
        {
            if (Settings.BufSizeMoreOrLess != value)
            {
                Settings.BufSizeMoreOrLess = value;
                OnPropertyChanged();
            }
        }
    }

    [RelayCommand]
    void SetStunSipgate()
    {
        StunServer = "stun.sipgate.net";
        StunPort = 3478;
    }

    [RelayCommand]
    void SetStunGoogle()
    {
        StunServer = "stun.l.google.com";
        StunPort = 19302;
    }
}
