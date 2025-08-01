﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    /// <summary>
    /// JWdP 20250726 Decided to outcomment CopyTo feature from UI, in order to simplify app usage
    /// so it's always CLIENTFROMSVR, but I keep functionality in the code
    /// </summary>
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
        /// FirstModeIsServer: this was the ModeIsServer at connectiontime; swap svr/cl does not change this variable
        /// </summary>
        public bool FirstModeIsServer { get; set; }

        /// <summary>
        /// UdpPort: -1=not set, 0 should not happen, > 0 = udpport from Stunserver;
        /// </summary>
        public int UdpPort { get; set; } = -1;
        public int UdpPortOtherside { get; set; } = -1;
        public string NATType { get; set; } = "";
        public string StrLocalIP { get; set; } = "";
        public string StrLocalIPSvr { get; set; } = "";     // server does not need to know localip of client
        public string StrPublicIp { get; set; } = "";
        public string StrPublicIpOtherside { get; set; } = "";
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
            UdpPort = -1;
            IpSvrThatClientConnectedTo = "";
            Connected = false;
            SvrPathParts.Clear();
            LocalPathPartsCl.Clear();
            CurrSvrFolders = new FileOrFolderData[0];
            CurrSvrFiles = new FileOrFolderData[0];
        }
    }
}
