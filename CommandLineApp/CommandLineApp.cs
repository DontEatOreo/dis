using System.CommandLine;
using System.CommandLine.Invocation;
using dis.CommandLineApp.Downloaders;
using Serilog;

namespace dis.CommandLineApp;

/// <summary>
/// Represents the main entry point for the command line application.
/// </summary>
public sealed class CommandLineApp
{
    private readonly ILogger _logger;
    private readonly Downloader _downloader;
    private readonly Converter _converter;
    private readonly CommandLineOptions _commandLineOptions;

    public CommandLineApp(Downloader downloader,
        Converter converter,
        CommandLineOptions commandLineOptions,
        ILogger logger)
    {
        _downloader = downloader;
        _converter = converter;
        _commandLineOptions = commandLineOptions;
        _logger = logger;
    }

    /// <summary>
    /// Executes the command line application using the provided arguments.
    /// </summary>
    /// <param name="args">The command line arguments to process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task Run(string[] args)
    {
        var (rootCommand, parsedOptions) = await _commandLineOptions.GetCommandLineOptions();
        rootCommand.SetHandler(context => RunHandler(context, parsedOptions));
        await rootCommand.InvokeAsync(args);
    }

    private async Task RunHandler(InvocationContext context, RunOptions o)
    {
        var parsed = ParseOptions(context, o);
        var links = parsed.Inputs.Where(video =>
                Uri.IsWellFormedUriString(video, UriKind.RelativeOrAbsolute) && !File.Exists(video))
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
            Crf = (int)parsed.Crf!,
            AudioBitRate = (int)parsed.AudioBitrate!,
            VideoCodec = parsed.VideoCodec
        };

        await ConvertVideosAsync(paths, settings);
    }

    private Task DownloadVideosAsync(IReadOnlyCollection<string> links, List<string> videoPaths, ParallelOptions parallelOptions, ParsedOptions o)
    {
        if (!links.Any())
            return Task.CompletedTask;

        Parallel.ForEach(links, parallelOptions, link =>
        {
            DownloadOptions downloadOptions = new()
            {
                Url = link,
                KeepWatermark = o.KeepWatermark,
                SponsorBlock = o.SponsorBlock
            };
            var (path, success) = _downloader.DownloadTask(downloadOptions).GetAwaiter().GetResult();

            if (!success)
                _logger.Error("Failed to download video: {Link}", link);
            else
                videoPaths.Add(path);
        });

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

        return Task.CompletedTask;
    }

    private static ParsedOptions ParseOptions(InvocationContext context, RunOptions o)
    {
        ParsedOptions options = new()
        {
            Inputs = context.ParseResult.GetValueForOption(o.Inputs)!,
            Resolution = context.ParseResult.GetValueForOption(o.Resolution),
            VideoCodec = context.ParseResult.GetValueForOption(o.VideoCodec),
            Output = context.ParseResult.GetValueForOption(o.Output)!,
            Crf = context.ParseResult.GetValueForOption(o.Crf) as int?,
            AudioBitrate = context.ParseResult.GetValueForOption(o.AudioBitrate) as int?
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
    }

}