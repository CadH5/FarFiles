using Android.App;
using Android.Content;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using FarFiles.Services;
using Microsoft.Maui.ApplicationModel;

namespace FarFiles.Platforms.Android
{
    public class AndroidFolderPicker : IAndroidFolderPicker
    {
        private TaskCompletionSource<global::Android.Net.Uri> _folderPickedTcs;

        public Task<global::Android.Net.Uri> PickFolderAsync()
        {
            var activity = Platform.CurrentActivity;

            if (activity == null)
                throw new InvalidOperationException("No current activity");

            _folderPickedTcs = new TaskCompletionSource<global::Android.Net.Uri>();

            var activityResultLauncher = ((IActivityResultRegistryOwner)activity).ActivityResultRegistry.Register(
                "folder-picker-key",
                new ActivityResultContracts.StartActivityForResult(),
                new FolderPickerResultCallback(_folderPickedTcs)
            );

            Intent intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantReadUriPermission);
            activityResultLauncher.Launch(intent);

            return _folderPickedTcs.Task;
        }

        private class FolderPickerResultCallback : Java.Lang.Object, IActivityResultCallback
        {
            private readonly TaskCompletionSource<global::Android.Net.Uri> _tcs;

            public FolderPickerResultCallback(TaskCompletionSource<global::Android.Net.Uri> tcs)
            {
                _tcs = tcs;
            }

            public void OnActivityResult(Java.Lang.Object result)
            {
                if (result is ActivityResult activityResult && activityResult.ResultCode == (int)Result.Ok)
                {
                    Intent data = activityResult.Data;
                    var uri = data?.Data;
                    if (uri != null)
                    {
                        _tcs.TrySetResult(uri);
                        return;
                    }
                }

                _tcs.TrySetException(new OperationCanceledException("Folder pick cancelled or failed"));
            }
        }
    }
}
