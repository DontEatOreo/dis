using System.CommandLine;
using System.CommandLine.Invocation;
using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;

namespace dis.CommandLineApp;

public sealed class CommandLineApp
{
    private readonly ILogger _logger;
    private readonly Globals _globals;
    private readonly IDownloader _downloader;
    private readonly Converter _converter;
    private readonly CommandLineOptions _commandLineOptions;

    public CommandLineApp(ILogger logger,
        Globals globals,
        IDownloader downloader,
        Converter converter,
        CommandLineOptions commandLineOptions)
    {
        _logger = logger;
        _globals = globals;
        _downloader = downloader;
        _converter = converter;
        _commandLineOptions = commandLineOptions;
    }

    public async Task Run(string[] args)
    {
        var (rootCommand, unParseOptions) = await _commandLineOptions.GetCommandLineOptions();
        rootCommand.SetHandler(context => RunHandler(context, unParseOptions));
        await rootCommand.InvokeAsync(args);
    }

    private async Task RunHandler(InvocationContext context, UnParseOptions options)
    {
        var parsed = ParseOptions(context, options);

        // On links list we ignore all files by check if they exist
        var links = parsed.Inputs.Where(video =>
                Uri.IsWellFormedUriString(video, UriKind.RelativeOrAbsolute) && File.Exists(video) is false)
            .Select(video => new Uri(video))
            .ToList();
        // And now we add them to Separate list
        var files = parsed.Inputs.Where(File.Exists).ToList();

        // We store all the downloaded videos here
        Dictionary<string, DateTime?> paths = new();
        await DownloadVideosAsync(links, paths, parsed);

        // Add existing files to the videoPaths list
        foreach (var file in files)
            paths.TryAdd(file, null);

        await ConvertVideosAsync(paths, parsed);
    }

    private async Task DownloadVideosAsync(IReadOnlyCollection<Uri> links, Dictionary<string, DateTime?> videos, ParsedOptions options)
    {
        if (!links.Any())
            return;

        var downloadTasks = links.Select(link =>
        {
            DownloadOptions downloadOptions = new(link, options.Trim, options.KeepWatermark, options.SponsorBlock);
            var (download, time) = _downloader.DownloadTask(downloadOptions).GetAwaiter().GetResult();
            if (download is null)
                _logger.Error("Failed to download video: {Link}", link);
            else
                videos.Add(download, time);
            return Task.CompletedTask;
        });

        foreach (var task in downloadTasks)
            await task;

        Console.WriteLine(); // New line after the download progress bar
        foreach (var path in videos.Keys)
        {
            // Converts the file size to a string with the appropriate unit
            var fileSize = new FileInfo(path).Length;
            var fileSizeStr = fileSize < 1024 * 1024
                ? $"{fileSize / 1024.0:F2} KiB"
                : $"{fileSize / 1024.0 / 1024.0:F2} MiB";
            _logger.Information(
                "Downloaded video to: {Path} | Size: {Size}", path, fileSizeStr);
        }
    }

    private static ParsedOptions ParseOptions(InvocationContext context, UnParseOptions o)
    {
        var inputs = context.ParseResult.GetValueForOption(o.Inputs)!;
        var output = context.ParseResult.GetValueForOption(o.Output)!;
        var crf = context.ParseResult.GetValueForOption(o.Crf);
        var resolution = context.ParseResult.GetValueForOption(o.Resolution!);
        var videoCodec = context.ParseResult.GetValueForOption(o.VideoCodec!);
        var trim = context.ParseResult.GetValueForOption(o.Trim!);

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
            Trim = trim,
            AudioBitrate = audioBitrate,
            RandomFileName = randomFileName,
            KeepWatermark = keepWaterMark,
            SponsorBlock = sponsorBlock
        };

        return options;
    }
    
    private async Task ConvertVideosAsync(IEnumerable<KeyValuePair<string, DateTime?>> videos, ParsedOptions options)
    {
        foreach (var (path, date) in videos)
        {
            try
            {
                await _converter.ConvertVideo(path, date, options);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to convert video: {Path}", path);
            }
        }
        
        _globals.DeleteLeftOvers();
    }
}
