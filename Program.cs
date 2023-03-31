using dis;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<Globals>();
        services.AddSingleton<FileExtensionContentTypeProvider>();
        services.AddSingleton<CommandLineApp>();
        services.AddSingleton<Progress>();
        services.AddSingleton<Converter>();
        services.AddSingleton<Downloader>();
    })
    .UseSerilog((hostingContext, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(hostingContext.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console();
    })
    .Build();

using var serviceScope = host.Services.CreateScope();
var services = serviceScope.ServiceProvider;

try
{
    var commandLineApp = services.GetRequiredService<CommandLineApp>();
    await commandLineApp.Run(args).ConfigureAwait(false);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while running the command line application");
}