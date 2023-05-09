using dis;
using dis.CommandLineApp;
using dis.CommandLineApp.Downloaders;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Expressions;
using Serilog.Settings.Configuration;

ConfigurationReaderOptions options = new(typeof(ConsoleLoggerConfigurationExtensions).Assembly, typeof(SerilogExpression).Assembly);

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<Globals>();
        services.AddSingleton<FileExtensionContentTypeProvider>();
        services.AddSingleton<CommandLineApp>();
        services.AddSingleton<CommandLineOptions>();

        services.AddTransient<Progress>();
        services.AddTransient<Converter>();
        services.AddTransient<Downloader>();
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
    logger.Error(ex, "An error occurred while running the app");
}