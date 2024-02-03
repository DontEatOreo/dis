using dis.CommandLineApp.Conversion;
using dis.CommandLineApp.Downloaders;
using dis.CommandLineApp.Interfaces;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace dis.CommandLineApp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        services.AddSingleton<Globals>();
        services.AddSingleton<FileExtensionContentTypeProvider>();
        services.AddSingleton<RootCommand>();
        services.AddSingleton<IDownloaderFactory, VideoDownloaderFactory>();

        services.AddTransient<CodecParser>();
        services.AddTransient<StreamConfigurator>();
        services.AddTransient<ProcessHandler>();
        services.AddTransient<PathHandler>();
        services.AddTransient<Converter>();
        services.AddTransient<LoggingLevelSwitch>();

        services.AddTransient<IDownloader, DownloadCreator>();
        services.AddSingleton<IDownloaderFactory>(sp =>
        {
            var globals = sp.GetRequiredService<Globals>();
            return new VideoDownloaderFactory(globals.YoutubeDl);
        });
        return services;
    }
}
