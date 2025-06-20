using Microsoft.Extensions.Logging;
using FarFiles.Services;
using FarFiles.View;
using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;
using System.Text;
using System;
using Microsoft.Maui.Media;

namespace FarFiles;

public static class MauiProgram
{
    public static string StrLocalIP { get; set; } = "";
    public static Settings Settings { get; set; } = new Settings();
    public static Info Info { get; set; } = new Info();
    public static Log Log { get; set; } = new Log();
    public static Tests Tests { get; set; } = new Tests();

#if ANDROID
    public static IAndroidFolderPicker AndroidFolderPicker =
                    new Platforms.Android.AndroidFolderPicker();
#endif

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
		builder.Services.AddSingleton<ClientViewModel>();

        builder.Services.AddSingleton<MainPageViewModel>();

        builder.Services.AddSingleton<AdvancedViewModel>();

        return builder.Build();
    }


    private async static void OnCloseThings()
    {
        MauiProgram.Info.AppIsShuttingDown = true;
        SaveSettings();

        try
        {
            if (Settings.ModeIsServer)           // server
            {
                var unregisterTask = Task<string>.Run(() => MauiProgram.PostToCentralServerAsync(
                    "UNREGISTER", MauiProgram.Info.UdpSvrPort, StrLocalIP, true));

                // Wait max 1 second — no deadlock risk
                unregisterTask.Wait(TimeSpan.FromSeconds(1));
 
                // (Note: if UNREGISTER fails somehow, the after a day the registration
                // also becomes invalid; see PHP).
            }

            Info.MainPageVwModel.OnCloseThings();
        }
        catch
        {
        }
    }



    public static async Task<string> PostToCentralServerAsync(string strCmd,
            int udpSvrPort, string strLocalIP, bool closing = false)
    {
        using (var client = new HttpClient())
        {
            var url = "https://www.cadh5.com/farfiles/farfiles.php";

            var requestData = new { Cmd = strCmd, ConnectKey = Settings.ConnectKey,
                UdpSvrPort = udpSvrPort, LocalIP = strLocalIP };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (closing)
            {
                // ChatGPT:
                // Optional: add a short timeout to HttpClient itself
                client.Timeout = TimeSpan.FromSeconds(2);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                // Only wait for headers, not full body
                await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

                return "";
            }
            else
            {
                HttpResponseMessage response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
        }
    }


    public static string ExcMsgWithInnerMsgs(Exception exc)
    {
        string retStr = exc.Message;
        while (exc.InnerException != null)
        {
            retStr += $"; inner: '{exc.InnerException.Message}'";
            exc = exc.InnerException;
        }

        return retStr;
    }




    private static void LoadSettings()
    {
#if ANDROID
        string androidUriRootAsStr = Preferences.Get("AndroidUriRoot", "");
        Settings.AndroidUriRoot = Android.Net.Uri.Parse(androidUriRootAsStr);
#else
        Settings.FullPathRoot = Preferences.Get("FullPathRoot", Settings.FullPathRoot);
#endif
        Settings.SvrClModeAsInt = Preferences.Get("SvrClModeAsInt", Settings.SvrClModeAsInt);
        Settings.Idx0isOverwr1isSkip = Preferences.Get("Idx0isOverwr1isSkip", Settings.Idx0isOverwr1isSkip);
        Settings.ConnectKey = Preferences.Get("ConnectKey", Settings.ConnectKey);
        Settings.StunServer = Preferences.Get("StunServer", Settings.StunServer);
        Settings.StunPort = Preferences.Get("StunPort", Settings.StunPort);
        Settings.TimeoutSecsClient = Preferences.Get("TimeoutSecsClient", Settings.TimeoutSecsClient);
        Settings.BufSizeMoreOrLess = Preferences.Get("BufSizeMoreOrLess", Settings.BufSizeMoreOrLess);
        Settings.UseSvrLocalIPClient = Preferences.Get("UseSvrLocalIPClient", Settings.UseSvrLocalIPClient);
    }
    public static void SaveSettings()
    {
#if ANDROID
        // JWdP 20250518 note: you are here already when on the Android phone the button
        // to see all minimized applications is hit, application is not even wiped away
        string androidUriRootAsStr = Settings.AndroidUriRoot?.ToString() ?? "";
        Preferences.Set("AndroidUriRoot", androidUriRootAsStr);
#else
        Preferences.Set("FullPathRoot", Settings.FullPathRoot);
#endif
        Preferences.Set("SvrClModeAsInt", Settings.SvrClModeAsInt);
        Preferences.Set("Idx0isOverwr1isSkip", Settings.Idx0isOverwr1isSkip);
        Preferences.Set("ConnectKey", Settings.ConnectKey);
        Preferences.Set("StunServer", Settings.StunServer);
        Preferences.Set("StunPort", Settings.StunPort);
        Preferences.Set("TimeoutSecsClient", Settings.TimeoutSecsClient);
        Preferences.Set("BufSizeMoreOrLess", Settings.BufSizeMoreOrLess);
        Preferences.Set("UseSvrLocalIPClient", Settings.UseSvrLocalIPClient);
    }
}
