//using Android.App;
using Android.Content;
using Android.OS;
using System;
using System.Timers;

namespace FarFiles.Platforms.Android
{
    //JEEWEE: CHATGPT:

    public class AppLifecycleTracker : Java.Lang.Object, global::Android.App.Application.IActivityLifecycleCallbacks
    {
        private int activityReferences = 0;
        private bool isActivityChangingConfigurations = false;
        private System.Timers.Timer backgroundTimer;

        public static Action? OnAppClosed;

        public void OnActivityResumed(global::Android.App.Activity activity)
        {
            activityReferences++;
            CancelBackgroundTimer();
        }

        public void OnActivityPaused(global::Android.App.Activity activity)
        {
            isActivityChangingConfigurations = activity.IsChangingConfigurations;
        }

        public void OnActivityStopped(global::Android.App.Activity activity)
        {
            activityReferences--;

            if (activityReferences == 0 && !isActivityChangingConfigurations)
            {
                StartBackgroundTimer();
            }
        }

        private void StartBackgroundTimer()
        {
            backgroundTimer = new System.Timers.Timer(3000); // wait 3 seconds before assuming app is gone
            backgroundTimer.Elapsed += (s, e) =>
            {
                backgroundTimer?.Stop();
                backgroundTimer?.Dispose();
                backgroundTimer = null;

                // App is now likely closed (not just backgrounded)
                OnAppClosed?.Invoke();
            };
            backgroundTimer.AutoReset = false;
            backgroundTimer.Start();
        }

        private void CancelBackgroundTimer()
        {
            if (backgroundTimer != null)
            {
                backgroundTimer.Stop();
                backgroundTimer.Dispose();
                backgroundTimer = null;
            }
        }

        public void OnActivityCreated(global::Android.App.Activity activity, Bundle? savedInstanceState) { }
        public void OnActivityStarted(global::Android.App.Activity activity) { }
        public void OnActivitySaveInstanceState(global::Android.App.Activity activity, Bundle outState) { }
        public void OnActivityDestroyed(global::Android.App.Activity activity) { }
    }
}
