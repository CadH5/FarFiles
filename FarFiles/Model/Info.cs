using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public class Info
    {
        public int UdpSvrPort { get; set; } = -1;   // -1=not set, 0=client, > 0 = svrport from Stunserver

        public string PublicIpSvrRegistered { get; set; } = "";
        public string PublicUdpPortSvrRegistered { get; set; } = "";
        public string LocalIpSvrRegistered { get; set; } = "";
        public string IpSvrThatClientConnectedTo { get; set; } = "";
        public bool Connected { get; set; } = false;
        public FileOrFolderData[] RootFolders { get; set; } = new FileOrFolderData[0];
        public FileOrFolderData[] RootFiles { get; set; } = new FileOrFolderData[0];
    }
}
