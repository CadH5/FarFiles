using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using FarFiles.Platforms.Android;

namespace FarFiles;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
    {
        //JEEWEE: THIS WAS CHATGPT's FIRST SUGGESTION, BUT DOESNT WORK
        //base.OnTrimMemory(level);

        //if (level == TrimMemory.Complete)
        //{
        //    // App was swiped away or is about to be killed
        //    MauiProgram.OnCloseThings(); // call your logic here
        //}
    }


    protected override void OnCreate(Bundle savedInstanceState)
    {
        //JEEWEE: CHATGPT:
        base.OnCreate(savedInstanceState);

        Application.RegisterActivityLifecycleCallbacks(new AppLifecycleTracker());

        AppLifecycleTracker.OnAppClosed = () =>
        {
            // Run your shutdown logic here
            MauiProgram.OnCloseThings();
        };
    }
}
