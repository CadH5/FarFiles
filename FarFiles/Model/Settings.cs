using FarFiles.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public enum SvrClMode
    {
        SERVER,
        SERVERWRITABLE,
        CLIENT,
    }

    public class Settings
    {
#if ANDROID
        public Android.Net.Uri AndroidUriRoot { get; set; } = null;
        public string FullPathRoot { get => AndroidUriRoot.Path; }    // no setter
#else
        public object AndroidUriRoot { get => null; }
        public string FullPathRoot { get; set; } = "";
#endif
        public int SvrClModeAsInt { get; set; } = (int)SvrClMode.SERVER;
        public bool ModeIsServer { get => SvrClModeAsInt == (int)SvrClMode.SERVER ||
                        SvrClModeAsInt == (int)SvrClMode.SERVERWRITABLE; }
        public int Idx0isOverwr1isSkip { get; set; } = 0;
        public string ConnectKey { get; set; } = "";

        public string StunServer { get; set; } = "stun.sipgate.net";
        public int StunPort { get; set; } = 3478;
        public int TimeoutSecsClient { get; set; } = 20;

        public int BufSizeMoreOrLess { get; set; } = 20000;
        public bool UseSvrLocalIPClient { get; set; } = false;

        public string PathFromRootAndSubPartsWindows(string[] subParts)
        {
            return FileDataService.PathFromRootAndSubPartsWindows(FullPathRoot, subParts);

//JEEWEE
//#if ANDROID
//            //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//            return "JEEWEE";
//#else
//            string path = FullPathRoot;
//            foreach (string subPathPart in subParts)
//                path = Path.Combine(path, subPathPart);
//            return path;
//#endif

        }
    }
}
