using Microsoft.Extensions.Logging;
using FarFiles.Services;
using FarFiles.View;
using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;
using System.Text;
using System;
//JEEWEE
//using ThreadNetwork;
using Microsoft.Maui.Media;

namespace FarFiles;

public static class MauiProgram
{
    public static int UdpSvrPort_0isclient { get; set; } = 0;
    public static string StrLocalIP { get; set; } = "";
    public static Settings Settings { get; set; } = new Settings();
    public static bool Connected { get; set; } = false;

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
                    OnCloseThings();
                }));
#endif
#if ANDROID
                events.AddAndroid(android => android.OnStop(activity =>
                {
                    OnCloseThings();
                }));
#endif
#if IOS || MACCATALYST
                events.AddiOS(iOS => iOS.WillTerminate((app) =>
                {
                    OnCloseThings();
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


    private async static void OnCloseThings()
    {
        SaveSettings();

        if (Settings.Idx0isSvr1isCl == 0)           // server
        {
            string msg = await MauiProgram.PostToCentralServerAsync(
                "UNREGISTER", UdpSvrPort_0isclient, StrLocalIP);

            // Funny, but during invoke of prev function the application ends, and
            // msg can never be read. Because of the await, I suppose (code after the wait
            // is not executed). But actually that's precisely what I wanted.
            // If I use msg = Task.Run(() => etc).Result, or use GetAwaiter, then
            // the application practically hangs.
            // (Note: if UNREGISTER fails somehow, the after a day the registration
            // also becomes invalid; see PHP).
        }
    }



    public static async Task<string> PostToCentralServerAsync(string strCmd,
            int udpSvrPort, string strLocalIP)
    {
        using (var client = new HttpClient())
        {
            var url = "https://www.cadh5.com/farfiles/farfiles.php";

            var requestData = new { Cmd = strCmd, ConnectKey = Settings.ConnectKey,
                UdpSvrPort = udpSvrPort, LocalIP = strLocalIP };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
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
