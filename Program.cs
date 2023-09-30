using System.Globalization;
using dis;
using dis.CommandLineApp;
using dis.CommandLineApp.Conversion;
using dis.CommandLineApp.Downloaders;
using dis.CommandLineApp.Interfaces;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;

var consoleLoggerConfigExtension = typeof(ConsoleLoggerConfigurationExtensions).Assembly;
var serilogExpression = typeof(SerilogExpression).Assembly;
ConfigurationReaderOptions options = new(consoleLoggerConfigExtension, serilogExpression);

LoggingLevelSwitch levelSwitch = new();
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<Globals>();
        services.AddSingleton<FileExtensionContentTypeProvider>();
        services.AddSingleton<ICommandLineApp, CommandLineApp>();
        services.AddSingleton<ICommandLineOptions, CommandLineOptions>();
        services.AddSingleton<ICommandLineValidator, CommandLineValidator>();
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
    })
    .UseSerilog((context, configuration) =>
    {
        configuration
            .MinimumLevel.ControlledBy(levelSwitch)
            .ReadFrom.Configuration(context.Configuration, options)
            .Enrich.FromLogContext()
            .WriteTo.Console();
    })
    .Build();

using var serviceScope = host.Services.CreateScope();
var services = serviceScope.ServiceProvider;

var commandLineApp = services.GetRequiredService<ICommandLineApp>();
var commandLineOptions = services.GetRequiredService<ICommandLineOptions>();

var (config, unParsedOptions) = commandLineOptions.GetCommandLineOptions();
var parseResult = config.Parse(args);
var parsedOptions = commandLineApp.ParseOptions(parseResult, unParsedOptions);

// Set the MinimumLevel property of the switch based on the argument value
levelSwitch.MinimumLevel = parsedOptions.Verbose ? LogEventLevel.Verbose : // Set the minimum level to Verbose
    LogEventLevel.Information;

try
{
    await commandLineApp.Handler(parsedOptions);
}
catch (Exception e)
{
    Log.Fatal(e, "An error occurred while running the app");
    throw;
}