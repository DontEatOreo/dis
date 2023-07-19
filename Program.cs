using dis;
using dis.CommandLineApp;
using dis.CommandLineApp.Downloaders;
using dis.CommandLineApp.Interfaces;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Expressions;
using Serilog.Settings.Configuration;

var consoleLoggerConfigExtension = typeof(ConsoleLoggerConfigurationExtensions).Assembly;
var serilogExpression = typeof(SerilogExpression).Assembly;
ConfigurationReaderOptions options = new(consoleLoggerConfigExtension, serilogExpression);

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<Globals>();
        services.AddSingleton<FileExtensionContentTypeProvider>();
        services.AddSingleton<CommandLineApp>();
        services.AddSingleton<CommandLineOptions>();
        services.AddSingleton<ICommandLineValidator, CommandLineValidator>();
        services.AddSingleton<IDownloaderFactory, VideoDownloaderFactory>();

        services.AddTransient<Progress>();
        services.AddTransient<Converter>();
        services.AddTransient<DownloadCreator>();
        services.AddSingleton<IDownloaderFactory>(sp =>
        {
            var globals = sp.GetRequiredService<Globals>();
            return new VideoDownloaderFactory(globals.YoutubeDl);
        });
    })
    .UseSerilog((context, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration, options)
            .Enrich.FromLogContext()
            .WriteTo.Console();
    })
    .Build();

using var serviceScope = host.Services.CreateScope();
var services = serviceScope.ServiceProvider;

try
{
    var commandLineApp = services.GetRequiredService<CommandLineApp>();
    await commandLineApp.Run(args);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger>();
    const string error = "An error occurred while running the app";
    logger.Error(ex, error);
}