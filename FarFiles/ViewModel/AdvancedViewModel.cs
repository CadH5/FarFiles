using STUN;

namespace FarFiles.ViewModel;

public partial class AdvancedViewModel : BaseViewModel
{
    string nl = Environment.NewLine;
    public string Info
    {
        get =>
            $"UdpSvrPort: {MauiProgram.Info.UdpSvrPort}{nl}" +
            $"PublicIpSvrRegistered: {MauiProgram.Info.PublicIpSvrRegistered}{nl}" +
            $"PublicUdpPortSvrRegistered: {MauiProgram.Info.PublicUdpPortSvrRegistered}{nl}" +
            $"LocalIpSvrRegistered: {MauiProgram.Info.LocalIpSvrRegistered}{nl}" +
            $"IpSvrThatClientConnectedTo: {MauiProgram.Info.IpSvrThatClientConnectedTo}";
    }

    public Settings Settings { get; protected set; } = MauiProgram.Settings;

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
