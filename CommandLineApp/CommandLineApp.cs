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
        var (rootCommand, parsedOptions) = await _commandLineOptions.GetCommandLineOptions();
        rootCommand.SetHandler(context => RunHandler(context, parsedOptions));
        await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Handles the invocation of the command line application with the given options.
    /// </summary>
    /// <param name="context">The invocation context.</param>
    /// <param name="o">The run options.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task RunHandler(InvocationContext context, RunOptions o)
    {
        var parsed = ParseOptions(context, o);
        var links = parsed.Inputs.Where(video =>
                Uri.IsWellFormedUriString(video, UriKind.RelativeOrAbsolute) && !File.Exists(video))
            .Select(video => new Uri(video))
            .ToList();
        var files = parsed.Inputs.Where(File.Exists).ToList();

        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };

        List<string> paths = new();
        await DownloadVideosAsync(links, paths, parallelOptions, parsed);

        // Add existing files to the videoPaths list
        paths.AddRange(files);

        VideoSettings settings = new()
        {
            Resolution = parsed.Resolution,
            GenerateRandomFileName = parsed.RandomFileName,
            OutputDirectory = parsed.Output,
            Crf = (int)parsed.Crf,
            AudioBitRate = (int)parsed.AudioBitrate!,
            VideoCodec = parsed.VideoCodec
        };

        await ConvertVideosAsync(paths, settings);
    }

    private async Task DownloadVideosAsync(IReadOnlyCollection<Uri> links, List<string> videoPaths, ParallelOptions parallelOptions, ParsedOptions o)
    {
        if (!links.Any())
            return;
        
        var downloadTasks = links.Select(link => {
            DownloadOptions options = new(link, o.KeepWatermark, o.SponsorBlock);
            var path = _downloadCreator.DownloadTask(options).GetAwaiter().GetResult();
            if (path is null)
                _logger.Error("Failed to download video: {Link}", link);
            else
                videoPaths.Add(path);
            return _downloadCreator.DownloadTask(options);
        });
        
        await Task.WhenAll(downloadTasks);

        Console.WriteLine(); // New Line after download success
        foreach (var path in videoPaths)
        {
            // Converts the file size to a string with the appropriate unit
            var fileSize = new FileInfo(path).Length;
            var fileSizeStr = fileSize < 1024 * 1024
                ? $"{fileSize / 1024.0:0.00} KiB"
                : $"{fileSize / 1024.0 / 1024.0:0.00} MiB";
            _logger.Information("Downloaded video to: {Path} | Size: {Size}", path, fileSizeStr);
        }

        return;
    }

    private static ParsedOptions ParseOptions(InvocationContext context, RunOptions o)
    {
        var inputs = context.ParseResult.GetValueForOption(o.Inputs);
        var resolution = context.ParseResult.GetValueForOption(o.Resolution);
        var videoCodec = context.ParseResult.GetValueForOption(o.VideoCodec);
        var output = context.ParseResult.GetValueForOption(o.Output);
        var crf = context.ParseResult.GetValueForOption(o.Crf);
        var audioBitrate = context.ParseResult.GetValueForOption(o.AudioBitrate);

        ParsedOptions options = new()
        {
            Inputs = inputs,
            Resolution = resolution,
            VideoCodec = videoCodec,
            Output = output,
            Crf = crf,
            AudioBitrate = audioBitrate,
        };

        if (o.RandomFilename is not null)
        {
            var randomFileName = context.ParseResult.GetValueForOption(o.RandomFilename);
            options.RandomFileName = randomFileName;
        }
        if (o.KeepWatermark is not null)
        {
            var keepWaterMark = context.ParseResult.GetValueForOption(o.KeepWatermark);
            options.KeepWatermark = keepWaterMark;
        }

        if (o.SponsorBlock is not null)
        {
            var sponsorBlock = context.ParseResult.GetValueForOption(o.SponsorBlock);
            options.SponsorBlock = sponsorBlock;
        }

        return options;
    }

    private async Task ConvertVideosAsync(IEnumerable<string> paths, VideoSettings settings)
    {
        foreach (var path in paths)
        {
            try
            {
                await _converter.ConvertVideo(path, settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to convert video: {Path}", path);
            }
        }
        _globals.DeleteLeftOvers();
    }
}