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
    //JEEWEE
    //public static int UdpSvrPort_0isclient { get; set; } = 0;
    public static string StrLocalIP { get; set; } = "";
    public static Settings Settings { get; set; } = new Settings();
    public static Info Info { get; set; } = new Info();
    public static Tests Tests { get; set; } = new Tests();

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

        builder.Services.AddSingleton<AdvancedViewModel>();
		//JEEWEE
        //builder.Services.AddSingleton<DetailsPage>();
		return builder.Build();
	}


    private async static void OnCloseThings()
    {
        SaveSettings();

        try
        {
            if (Settings.Idx0isSvr1isCl == 0)           // server
            {
                var unregisterTask = Task<string>.Run(() => MauiProgram.PostToCentralServerAsync(
                    "UNREGISTER", MauiProgram.Info.UdpSvrPort, StrLocalIP, true));

                // Wait max 1 second — no deadlock risk
                unregisterTask.Wait(TimeSpan.FromSeconds(1));
 
                // (Note: if UNREGISTER fails somehow, the after a day the registration
                // also becomes invalid; see PHP).
            }
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
        Settings.FullPathRoot = Preferences.Get("FullPathRoot", Settings.FullPathRoot);
        Settings.Idx0isSvr1isCl = Preferences.Get("Idx0isSvr1isCl", Settings.Idx0isSvr1isCl);
        Settings.ConnectKey = Preferences.Get("ConnectKey", Settings.ConnectKey);
        Settings.StunServer = Preferences.Get("StunServer", Settings.StunServer);
        Settings.StunPort = Preferences.Get("StunPort", Settings.StunPort);
    }
    private static void SaveSettings()
    {
        Preferences.Set("FullPathRoot", Settings.FullPathRoot);
        Preferences.Set("Idx0isSvr1isCl", Settings.Idx0isSvr1isCl);
        Preferences.Set("ConnectKey", Settings.ConnectKey);
        Preferences.Set("StunServer", Settings.StunServer);
        Preferences.Set("StunPort", Settings.StunPort);
    }
}
