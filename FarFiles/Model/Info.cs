using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public enum CpClientToFromMode
    {
        CLIENTFROMSVR,
        CLIENTTOSVR,
    }
    public enum FfState
    {
        UNREGISTERED,
        REGISTERED,
        CONNECTED,
        INTRANSACTION,
    }
    public class Info
    {
        public MainPageViewModel MainPageVwModel { get; set; }
        public ClientViewModel ClientPageVwModel { get; set; }

        /// <summary>
        /// FirstModeIsServer: this was the ModeIsServer at connectiontime; swap svr/cl does not change this variable
        /// </summary>
        public bool FirstModeIsServer { get; set; }

        /// <summary>
        /// UdpPort: -1=not set, 0=no udp but id (communication by Central Server), > 0 = udpport from Stunserver;
        /// </summary>
        public int UdpPort { get; set; } = -1;
        public int UdpPortOtherside { get; set; } = -1;
        public string IdInsteadOfUdp = "";
        public string IdInsteadOfUdpOtherSide = "";

        /// <summary>
        /// RegisteredCode: from Central Server; 0=none, 1=server, 2=server+client
        /// </summary>
        public string RegisteredCode = "0";

        public string NATType { get; set; } = "";
        public string StrLocalIP { get; set; } = "";
        public string StrLocalIPSvr { get; set; } = "";     // server does not need to know localip of client
        public string StrPublicIp { get; set; } = "";
        public string StrPublicIpOtherside { get; set; } = "";
        public string IpSvrThatClientConnectedTo { get; set; } = "";
        public bool IsSvrWritableReportedToClient { get; set; } = false;
        public CpClientToFromMode CpClientToFromMode { get; set; } = CpClientToFromMode.CLIENTFROMSVR;
        public bool AppIsShuttingDown { get; set; } = false;
        public int NumAnswersSent { get; set; } = 0;
        public List<string> SvrPathParts { get; set; } = new List<string>();
        public List<string> LocalPathPartsCl { get; set; } = new List<string>();
        public FileOrFolderData[] CurrSvrFolders { get; set; } = new FileOrFolderData[0];
        public FileOrFolderData[] CurrSvrFiles { get; set; } = new FileOrFolderData[0];
        public FfState FfState { get; set; } = FfState.UNREGISTERED;

        /// <summary>
        /// SvrReceivedClientGuid: once server has received the client guid, it remembers it
        /// (Info is not persistent), and if it receives message from other device, it rejects
        /// </summary>
        public Guid SvrReceivedClientGuid { get; set; } = Guid.Empty;


        public void SetUdpOrId(string udpOrId, bool otherSide)
        {
            int udpPort = udpOrId.Length == 32 ? 0 : Convert.ToInt32(udpOrId);
            string idInsteadOfUdp = udpOrId.Length == 32 ? udpOrId : "";
            if (otherSide)
            {
                UdpPortOtherside = udpPort;
                IdInsteadOfUdpOtherSide = udpOrId;
            }
            else
            {
                UdpPort = udpPort;
                IdInsteadOfUdp = udpOrId;
            }
        }

        public void DisconnectOnClient()
        {
            UdpPort = -1;
            IdInsteadOfUdp = "";
            IpSvrThatClientConnectedTo = "";
            FfState = FfState.UNREGISTERED;
            SvrPathParts.Clear();
            LocalPathPartsCl.Clear();
            CurrSvrFolders = new FileOrFolderData[0];
            CurrSvrFiles = new FileOrFolderData[0];
        }
    }
}
