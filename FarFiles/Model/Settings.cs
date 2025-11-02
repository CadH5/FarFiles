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
        CLIENT,
        SERVER,
        SERVERWRITABLE,
    }

    public enum CommunicMode
    {
        LOCALIP,
        NATHOLEPUNCHING,
        CENTRALSVR,
    }

    public class Settings
    {
#if ANDROID
        public Android.Net.Uri AndroidUriRoot { get; set; } = null;
        public string FullPathRoot { get => AndroidUriRoot?.Path ?? ""; }    // no setter
#else
        public object AndroidUriRoot { get => null; }
        public string FullPathRoot { get; set; } = "";
#endif
        public int SvrClModeAsInt { get; set; } = (int)SvrClMode.SERVER;
        public int CommunicModeAsInt { get; set; } = (int)CommunicMode.LOCALIP;

        /// <summary>
        /// ModeIsServer: true if SvrClModeAsInt is (int)SvrClMode.SERVER or (int)SvrClMode.SERVERWRITABLE
        /// </summary>
        public bool ModeIsServer { get => SvrClModeAsInt == (int)SvrClMode.SERVER ||
                        SvrClModeAsInt == (int)SvrClMode.SERVERWRITABLE; }
        public int Idx0isOverwr1isSkip { get; set; } = 0;
        public string ConnectKey { get; set; } = "";

        public string StunServer { get; set; } = "stun.sipgate.net";
        public int StunPort { get; set; } = 3478;
        public int TimeoutSecsClient { get; set; } = 20;

        public int BufSizeMoreOrLess { get; set; } = 20000;

        /// <summary>
        /// ConnectionGuid: determined by Client, first time when not yet determined.
        /// Once server receives a guid, in this session it won't accept another one, to avoid intruders.
        /// If client app is restarted and the server is still active, it can reconnect because the clientguid is persistent.
        /// </summary>
        public Guid ConnClientGuid { get; set; } = Guid.Empty;
        public FfState LastKnownState { get; set; } = FfState.UNREGISTERED;

#if ANDROID
#else
        public string PathFromRootAndSubPartsWindows(string[] subParts)
        {
            return FileDataService.PathFromRootAndSubPartsWindows(FullPathRoot, subParts);
        }
#endif
    }
}
