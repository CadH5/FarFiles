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
        public int UdpSvrPort { get; set; } = -1;   // -1=not set, 0=client, > 0 = svrport from Stunserver

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
