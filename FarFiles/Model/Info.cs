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
    public class Info
    {
        public MainPageViewModel MainPageVwModel { get; set; }
        public ClientViewModel ClientPageVwModel { get; set; }

        /// <summary>
        /// UdpSvrPort: -1=not set, 0=client, > 0 = svrport from Stunserver;
        /// if svr/client swap, then client has the > 0 port instead of the server
        /// </summary>
        public int UdpSvrPort { get; set; } = -1;

        public string PublicIpSvrRegistered { get; set; } = "";
        public string PublicUdpPortSvrRegistered { get; set; } = "";
        public string LocalIpSvrRegistered { get; set; } = "";
        public string IpSvrThatClientConnectedTo { get; set; } = "";
        public bool IsSvrWritableReportedToClient { get; set; } = false;
        public CpClientToFromMode CpClientToFromMode { get; set; } = CpClientToFromMode.CLIENTFROMSVR;
        public bool Connected { get; set; } = false;
        public bool AppIsShuttingDown { get; set; } = false;
        public int NumAnswersSent { get; set; } = 0;
        public List<string> SvrPathParts { get; set; } = new List<string>();
        public List<string> LocalPathPartsCl { get; set; } = new List<string>();
        public FileOrFolderData[] CurrSvrFolders { get; set; } = new FileOrFolderData[0];
        public FileOrFolderData[] CurrSvrFiles { get; set; } = new FileOrFolderData[0];

        /// <summary>
        /// SvrReceivedClientGuid: once server has received the client guid, it remembers it
        /// (Info is not persistent), and if it receives message from other device, it rejects
        /// </summary>
        public Guid SvrReceivedClientGuid { get; set; } = Guid.Empty;


        public void DisconnectOnClient()
        {
            UdpSvrPort = -1;
            IpSvrThatClientConnectedTo = "";
            Connected = false;
            SvrPathParts.Clear();
            LocalPathPartsCl.Clear();
            CurrSvrFolders = new FileOrFolderData[0];
            CurrSvrFiles = new FileOrFolderData[0];
        }
    }
}
