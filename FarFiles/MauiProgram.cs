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
        LoadSettings_donotforgettoaddnewsetting();

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
        SaveSettings_donotforgettoaddnewsetting();

        try
        {
            if (MauiProgram.Info.FirstModeIsServer)           // originally the server, but svr/client may have swapped
            {
                var unregisterTask = Task<string>.Run(() => MauiProgram.PostToCentralServerAsync(
                    "UNREGISTER", true));

                // Wait max 1 second — no deadlock risk
                unregisterTask.Wait(TimeSpan.FromSeconds(1));
 
                // (Note: if UNREGISTER fails somehow, then after a day the registration
                // also becomes invalid; see PHP).
            }

            Info.MainPageVwModel.OnCloseThings();
        }
        catch
        {
        }
    }


    /// <summary>
    /// Actually, a requirement is that the enum has contiguous int values
    /// </summary>
    /// <param name="valAsStr"></param>
    /// <param name="valAsInt"></param>
    /// <returns></returns>
    public static bool ParseIntEnum<T>(string valAsStr, out int valAsInt) where T : System.Enum
    {
        if (! int.TryParse(valAsStr, out valAsInt))
        {
            valAsInt = 0;
            return false;
        }
        if (valAsInt < 0 || valAsInt >= Enum.GetNames(typeof(T)).Length)
        {
            valAsInt = 0;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Actually, a requirement is that the enum has contiguous int values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="valAsInt"></param>
    /// <returns></returns>
    public static string IntEnumValToString<T>(int valAsInt) where T : System.Enum
    {
        if (ParseIntEnum<T>(valAsInt.ToString(), out int dummyInt))
            return Enum.GetNames(typeof(T))[valAsInt];

        return "";
    }

    public static async Task<string> PostToCentralServerAsync(string strCmd, bool closing = false)
    {
        using (var client = new HttpClient())
        {
            //some measures, suggested by ChatGPT, to avoid landing at a page for robot visitors, we want a Json response:
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            client.DefaultRequestHeaders.ConnectionClose = false;

            var url = "https://www.cadh5.com/farfiles/farfiles.php";

            var requestData = new { Cmd = strCmd, ConnectKey = Settings.ConnectKey,
                UdpPort = MauiProgram.Info.UdpPort, LocalIP = MauiProgram.Info.StrLocalIP,
                IsSvr0Client1 = MauiProgram.Info.FirstModeIsServer ? 0 : 1,
                CommunicModeAsInt = MauiProgram.Settings.CommunicModeAsInt,
            };

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

                string retStr = await response.Content.ReadAsStringAsync();
                retStr = retStr.Trim();

                if (!retStr.StartsWith('{'))
                {
                    string start = retStr.Length <= 30 ? retStr : retStr.Substring(30) + " ...";
                    throw new Exception($"Respomse from central server: not Json format ('{start}')");
                }

                return retStr;
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




    private static void LoadSettings_donotforgettoaddnewsetting()
    {
#if ANDROID
        string androidUriRootAsStr = Preferences.Get("AndroidUriRoot", "");
        Settings.AndroidUriRoot = Android.Net.Uri.Parse(androidUriRootAsStr);
#else
        Settings.FullPathRoot = Preferences.Get("FullPathRoot", Settings.FullPathRoot);
#endif
        Settings.SvrClModeAsInt = Preferences.Get("SvrClModeAsInt", Settings.SvrClModeAsInt);
        Settings.CommunicModeAsInt = Preferences.Get("CommunicModeAsInt", Settings.CommunicModeAsInt);
        Settings.Idx0isOverwr1isSkip = Preferences.Get("Idx0isOverwr1isSkip", Settings.Idx0isOverwr1isSkip);
        Settings.ConnectKey = Preferences.Get("ConnectKey", Settings.ConnectKey);
        Settings.StunServer = Preferences.Get("StunServer", Settings.StunServer);
        Settings.StunPort = Preferences.Get("StunPort", Settings.StunPort);
        Settings.TimeoutSecsClient = Preferences.Get("TimeoutSecsClient", Settings.TimeoutSecsClient);
        Settings.BufSizeMoreOrLess = Preferences.Get("BufSizeMoreOrLess", Settings.BufSizeMoreOrLess);
        //JEEWEE
        //Settings.UseSvrLocalIP = Preferences.Get("UseSvrLocalIP", Settings.UseSvrLocalIP);
        try
        {
            Settings.ConnClientGuid = Guid.Parse(Preferences.Get("ConnClientGuid", Settings.ConnClientGuid.ToString()));
        }
        catch
        {
            Settings.ConnClientGuid = Guid.Empty;
        }
    }
    public static void SaveSettings_donotforgettoaddnewsetting()
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
        Preferences.Set("CommunicModeAsInt", Settings.CommunicModeAsInt);
        Preferences.Set("Idx0isOverwr1isSkip", Settings.Idx0isOverwr1isSkip);
        Preferences.Set("ConnectKey", Settings.ConnectKey);
        Preferences.Set("StunServer", Settings.StunServer);
        Preferences.Set("StunPort", Settings.StunPort);
        Preferences.Set("TimeoutSecsClient", Settings.TimeoutSecsClient);
        Preferences.Set("BufSizeMoreOrLess", Settings.BufSizeMoreOrLess);
        //JEEWEE
        //Preferences.Set("UseSvrLocalIP", Settings.UseSvrLocalIP);
        Preferences.Set("ConnClientGuid", Settings.ConnClientGuid.ToString());
    }
}
