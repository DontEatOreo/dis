using System.Globalization;
using dis.CommandLineApp;
using dis.CommandLineApp.Interfaces;
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

HostBuilder hostBuilder = new();
hostBuilder.ConfigureServices((_, services) => services.AddMyServices());
hostBuilder.UseSerilog((context, configuration) =>
    configuration
        .MinimumLevel.ControlledBy(levelSwitch)
        .ReadFrom.Configuration(context.Configuration, options)
        .Enrich.FromLogContext()
        .WriteTo.Console());

using var host = hostBuilder.Build();
using var serviceScope = host.Services.CreateScope();
var services = serviceScope.ServiceProvider;

var commandLineApp = services.GetRequiredService<ICommandLineApp>();
var commandLineOptions = services.GetRequiredService<ICommandLineOptions>();

var (config, parsedOptions) = commandLineOptions.GetCommandLineOptions();

// Set the MinimumLevel property of the switch based on the argument value
levelSwitch.MinimumLevel = parsedOptions.Verbose ? LogEventLevel.Verbose : // Set the minimum level to Verbose
    LogEventLevel.Information;

CancellationTokenSource cancellationTokenSource = new();
Console.CancelKeyPress += (_, _) => { cancellationTokenSource.Cancel(); };

try
{
    config.RootCommand.SetAction(async (_, _) => await commandLineApp.Handler(parsedOptions));
    await config.InvokeAsync(args, cancellationTokenSource.Token);
}
catch (Exception e)
{
    Log.Fatal(e, "An error occurred while running the app");
    throw;
}
