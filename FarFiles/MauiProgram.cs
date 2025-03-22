using Microsoft.Extensions.Logging;
using FarFiles.Services;
using FarFiles.View;
using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;

namespace FarFiles;

public static class MauiProgram
{
    public static Settings Settings { get; set; } = new Settings();

	public static MauiApp CreateMauiApp()
	{
        LoadSettings();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			})

            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(w => w.OnClosed((window, args) =>
                {
                    SaveSettings();
                }));
#endif
#if ANDROID
                events.AddAndroid(android => android.OnStop(activity =>
                {
                    SaveSettings();
                }));
#endif
#if IOS || MACCATALYST
                events.AddiOS(iOS => iOS.WillTerminate((app) =>
                {
                    SaveSettings();
                }));
#endif
            });



#if DEBUG
        //JEEWEE
        //builder.Logging.AddDebug();
#endif



        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
		builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
		builder.Services.AddSingleton<IMap>(Map.Default);

		builder.Services.AddSingleton<FileDataService>();
		builder.Services.AddSingleton<FilesViewModel>();
        //JEEWEE
        //builder.Services.AddSingleton<SettingsService>();

        builder.Services.AddSingleton<MainPageViewModel>();

        builder.Services.AddTransient<FileDetailsViewModel>();
		builder.Services.AddSingleton<DetailsPage>();
		return builder.Build();
	}


    private static void LoadSettings()
    {
        Settings.FullPathRoot = Preferences.Get("FullPathRoot", Settings.FullPathRoot);
        Settings.Idx0isSvr1isCl = Preferences.Get("Idx0isSvr1isCl", Settings.Idx0isSvr1isCl);
        Settings.ConnectKey = Preferences.Get("ConnectKey", Settings.ConnectKey);
    }
    private static void SaveSettings()
    {
        Preferences.Set("FullPathRoot", Settings.FullPathRoot);
        Preferences.Set("Idx0isSvr1isCl", Settings.Idx0isSvr1isCl);
        Preferences.Set("ConnectKey", Settings.ConnectKey);
    }
}
