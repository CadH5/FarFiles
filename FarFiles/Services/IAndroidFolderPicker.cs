using System.Threading.Tasks;

#if ANDROID
using Android.Net;
#endif

namespace FarFiles.Services
{
    public interface IAndroidFolderPicker
    {
#if ANDROID
        Task<Android.Net.Uri> PickFolderAsync();
#endif
    }
}
