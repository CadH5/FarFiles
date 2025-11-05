using STUN;

namespace FarFiles.ViewModel;

public partial class AdvancedViewModel : BaseViewModel
{
    string nl = Environment.NewLine;
    public string Info
    {
        get
        {
            bool udp = MauiProgram.Settings.CommunicModeAsInt != (int)CommunicMode.CENTRALSVR;
            bool svr = MauiProgram.Settings.ModeIsServer;
            
            return
            (udp ?
            $"UdpPort: {MauiProgram.Info.UdpPort}{nl}" +
            $"UdpPort other side: {MauiProgram.Info.UdpPortOtherside}{nl}"
            :
            $"IdInsteadOfUdp: {MauiProgram.Info.IdInsteadOfUdp}{nl}" +
            $"IdInsteadOfUdp other side: {MauiProgram.Info.IdInsteadOfUdpOtherSide}{nl}"
            ) +
            $"Public IP: {MauiProgram.Info.StrPublicIp}{nl}" +
            $"Public IP other side: {MauiProgram.Info.StrPublicIpOtherside}{nl}" +
            $"Local IP: {MauiProgram.Info.StrLocalIP}{nl}" +
            $"Local IP server: {MauiProgram.Info.StrLocalIPSvr}{nl}" +
            $"Communication mode: {MauiProgram.IntEnumValToString<CommunicMode>(MauiProgram.Settings.CommunicModeAsInt)}{nl}" +
            (! svr ?
            $"IP Svr That Client Connected To: {MauiProgram.Info.IpSvrThatClientConnectedTo}{nl}"
            : "") +
            (udp ?
            $"NATtype from stun server: {MauiProgram.Info.NATType}{nl}"
            : "") +
            $"State: {MauiProgram.Info.FfState}";
        }
    }

    public Settings Settings { get; protected set; } = MauiProgram.Settings;

    public bool SettingsSvrVis { get => MauiProgram.Settings.ModeIsServer; }
    public bool SettingsClientVis { get => !MauiProgram.Settings.ModeIsServer; }
    public bool SettingsUdpVis { get => MauiProgram.Settings.CommunicModeAsInt != (int)CommunicMode.CENTRALSVR; }

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

    public void OnPageAppearing()
    {
        OnPropertyChanged();
        OnPropertyChanged(nameof(SettingsSvrVis));
        OnPropertyChanged(nameof(SettingsClientVis));
        OnPropertyChanged(nameof(SettingsUdpVis));
        OnPropertyChanged(nameof(Info));
    }


}
