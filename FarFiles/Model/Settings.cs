using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public class Settings
    {
        public string FullPathRoot { get; set; } = "";
        public int Idx0isSvr1isCl { get; set; } = 0;
        public string ConnectKey { get; set; } = "";

        public string StunServer { get; set; } = "stun.sipgate.net";
        public int StunPort { get; set; } = 3478;



        public string PathFromRootAndSubParts(string[] subParts)
        {
            string path = FullPathRoot;
            foreach (string subPathPart in subParts)
                path = Path.Combine(path, subPathPart);
            return path;
        }
    }
}
