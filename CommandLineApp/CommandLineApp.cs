using System.CommandLine;
using System.CommandLine.Invocation;
using dis.CommandLineApp.Downloaders;
using dis.CommandLineApp.Models;
using Serilog;

namespace dis.CommandLineApp;

public sealed class CommandLineApp
{
    private readonly ILogger _logger;
    private readonly Globals _globals;
    private readonly DownloadCreator _downloadCreator;
    private readonly Converter _converter;
    private readonly CommandLineOptions _commandLineOptions;

    public CommandLineApp(ILogger logger,
        Globals globals,
        DownloadCreator downloadCreator,
        Converter converter,
        CommandLineOptions commandLineOptions)
    {
        _logger = logger;
        _globals = globals;
        _downloadCreator = downloadCreator;
        _converter = converter;
        _commandLineOptions = commandLineOptions;
    }
    
    public async Task Run(string[] args)
    {
        var (rootCommand, unParseOptions) = await _commandLineOptions.GetCommandLineOptions();
        rootCommand.SetHandler(context => RunHandler(context, unParseOptions));
        await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Handles the invocation of the command line application with the given options.
    /// </summary>
    /// <param name="context">The invocation context.</param>
    /// <param name="options">The run options.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task RunHandler(InvocationContext context, UnParseOptions options)
    {
        var parsed = ParseOptions(context, options);
        var links = parsed.Inputs.Where(video =>
                Uri.IsWellFormedUriString(video, UriKind.RelativeOrAbsolute) && !File.Exists(video))
            .Select(video => new Uri(video))
            .ToList();
        var files = parsed.Inputs.Where(File.Exists).ToList();
        
        List<string> paths = new();
        await DownloadVideosAsync(links, paths, parsed);

        // Add existing files to the videoPaths list
        paths.AddRange(files);

        await ConvertVideosAsync(paths, parsed);
    }

    private async Task DownloadVideosAsync(IReadOnlyCollection<Uri> links, List<string> videoPaths, ParsedOptions options)
    {
        if (!links.Any())
            return;
        
        var downloadTasks = links.Select(link => {
            DownloadOptions downloadOptions = new(link, options.KeepWatermark, options.SponsorBlock);
            var path = _downloadCreator.DownloadTask(downloadOptions).GetAwaiter().GetResult();
            if (path is null)
                _logger.Error("Failed to download video: {Link}", link);
            else
                videoPaths.Add(path);
            return _downloadCreator.DownloadTask(downloadOptions);
        });
        
        await Task.WhenAll(downloadTasks);
        
        foreach (var path in videoPaths)
        {
            // Converts the file size to a string with the appropriate unit
            var fileSize = new FileInfo(path).Length;
            var fileSizeStr = fileSize < 1024 * 1024
                ? $"{fileSize / 1024.0:0.00} KiB"
                : $"{fileSize / 1024.0 / 1024.0:0.00} MiB";
            _logger.Information("Downloaded video to: {Path} | Size: {Size}", path, fileSizeStr);
        }
    }

    private static ParsedOptions ParseOptions(InvocationContext context, UnParseOptions o)
    {
        var inputs = context.ParseResult.GetValueForOption(o.Inputs)!;
        var output = context.ParseResult.GetValueForOption(o.Output)!;
        var crf = context.ParseResult.GetValueForOption(o.Crf);
        var resolution = context.ParseResult.GetValueForOption(o.Resolution);
        var videoCodec = context.ParseResult.GetValueForOption(o.VideoCodec);
        var audioBitrate = context.ParseResult.GetValueForOption(o.AudioBitrate);
        
        var randomFileName = context.ParseResult.GetValueForOption(o.RandomFileName);
        var keepWaterMark = context.ParseResult.GetValueForOption(o.KeepWatermark);
        var sponsorBlock = context.ParseResult.GetValueForOption(o.SponsorBlock);

        ParsedOptions options = new()
        {
            Inputs = inputs,
            Output = output,
            Crf = crf,
            Resolution = resolution,
            VideoCodec = videoCodec,
            AudioBitrate = audioBitrate,
            RandomFileName = randomFileName,
            KeepWatermark = keepWaterMark,
            SponsorBlock = sponsorBlock
        };

        return options;
    }

    private async Task ConvertVideosAsync(IEnumerable<string> paths, ParsedOptions options)
    {
        foreach (var path in paths)
        {
            try
            {
                await _converter.ConvertVideo(path, options);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to convert video: {Path}", path);
            }
        }
        _globals.DeleteLeftOvers();
    }
}
