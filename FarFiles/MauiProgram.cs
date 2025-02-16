using Microsoft.Extensions.Logging;
using FarFiles.Services;
using FarFiles.View;
using CommunityToolkit.Maui;

namespace FarFiles;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
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

        builder.Services.AddSingleton<MainPageViewModel>();

        builder.Services.AddTransient<FileDetailsViewModel>();
		builder.Services.AddSingleton<DetailsPage>();
		return builder.Build();
	}
}
