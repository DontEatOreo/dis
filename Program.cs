using System.Globalization;
using dis.CommandLineApp;
using dis.CommandLineApp.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Expressions;
using Serilog.Settings.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

if (args.Length is 0) args = ["--help"];

var consoleLoggerConfigExtension = typeof(ConsoleLoggerConfigurationExtensions).Assembly;
var serilogExpression = typeof(SerilogExpression).Assembly;
ConfigurationReaderOptions options = new(consoleLoggerConfigExtension, serilogExpression);

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

IServiceCollection collection = new ServiceCollection();
HostBuilder hostBuilder = new();
hostBuilder.ConfigureServices((_, services) =>
{
    services.AddMyServices();
    collection = services;
});
hostBuilder.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration, options)
        .Enrich.FromLogContext()
        .WriteTo.Console());
hostBuilder.Build();

var registrar = new TypeRegistrar(collection);

var app = new CommandApp<RootCommand>(registrar);
app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
    config.AddExample("-i", "https://youtu.be/hT_nvWreIhg");
    config.AddExample("-i", "https://youtu.be/hT_nvWreIhg", "-t", "73.25-110");
    config.AddExample("-i", "https://youtu.be/hT_nvWreIhg", "-o",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)));
    config.SetApplicationName("dis");
});

try
{
    await app.RunAsync(args);
}
catch (CommandAppException e)
{
    AnsiConsole.WriteLine(e.Message);
    throw;
}
