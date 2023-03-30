using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.AspNetCore.StaticFiles;
using Pastel;
using Serilog;

namespace dis;

public class CommandLineApp
{
    #region Constructor

    private readonly ILogger _logger;
    private readonly FileExtensionContentTypeProvider _provider;
    private readonly Downloader _downloader;
    private readonly Converter _converter;
    private readonly Globals _globals;

    public CommandLineApp(ILogger logger,
        FileExtensionContentTypeProvider provider,
        Downloader downloader,
        Converter converter,
        Globals globals)
    {
        _logger = logger;
        _provider = provider;
        _downloader = downloader;
        _converter = converter;
        _globals = globals;
    }

    #endregion

    #region Methods

    public async Task Run(string[] args)
    {
        RootCommand rootCommand = new();

        Option<bool> randomFilenameOption =
            new(new[] { "-rn", "-rd", "-rnd", "--random" },
                "Randomize the filename");
        Option<bool> keepWatermarkOption =
            new(new[] { "-k", "-kw", "-kwm", "--keep" },
                "Keep the watermark");
        Option<bool> sponsorBlockOption =
            new(new[] { "-sb", "-sponsorblock", "--sponsorblock" },
                "Remove the sponsorblock from the video");
        keepWatermarkOption.SetDefaultValue(false);

        Option<string[]> inputOption =
            new(new[] { "-i", "--input", "-f", "--file" },
                    "A path to a video file or a link to a video")
                { AllowMultipleArgumentsPerToken = true, IsRequired = true };
        inputOption.AddValidator(validate =>
        {
            var value = validate.GetValueOrDefault<string[]>();
            if (value is null)
            {
                Console.Error.WriteLine("No input file or link was provided".Pastel(ConsoleColor.Red));
                Environment.Exit(1);
            }

            foreach (var item in value)
            {
                if (File.Exists(item) || Uri.IsWellFormedUriString(item, UriKind.RelativeOrAbsolute))
                    continue;
                Console.Error.WriteLine("Invalid input file or link".Pastel(ConsoleColor.Red));
                Environment.Exit(1);
            }
        });

        Option<string> outputOption =
            new(new[] { "-o", "--output" },
                "Directory to save the compressed video to\n");
        outputOption.SetDefaultValue(Environment.CurrentDirectory);
        outputOption.AddValidator(validate =>
        {
            var outputValue = validate.GetValueOrDefault<string>();
            if (Directory.Exists(outputValue))
                return;
            Console.Error.WriteLine("Output directory does not exist".Pastel(ConsoleColor.Red));
            Environment.Exit(1);
        });

        Option<string> videoCodecOption =
            new(new[] { "-vc", "--codec", "--video-codec" },
                "Video codec");
        foreach (var key in _globals.ValidVideoCodesMap.Keys)
            videoCodecOption.AddCompletions(key);
        videoCodecOption.AddValidator(validate =>
        {
            var videoCodecValue = validate.GetValueOrDefault<string>()?.Trim();
            if (videoCodecValue is null)
                return;
            if (_globals.ValidVideoCodesMap.ContainsKey(videoCodecValue))
                return;
            Console.Error.WriteLine("Invalid video codec".Pastel(ConsoleColor.Red));
            Environment.Exit(1);
        });

        Option<int> crfInput =
            new(new[] { "-c", "--crf" },
                "CRF value");
        crfInput.SetDefaultValue(29);
        crfInput.AddValidator(validate =>
        {
            var crfValue = validate.GetValueOrDefault<int>();
            if (crfValue is >= 0 and <= 63)
                return;
            Console.Error.WriteLine("CRF value must be between 0 and 63 (Avoid values below 20)".Pastel(ConsoleColor.Red));
            Environment.Exit(1);
        });

        Option<string> resolutionOption =
            new(new[] { "-r", "--resolution" },
                "Resolution");
        resolutionOption.AddCompletions(_globals.ResolutionList);
        resolutionOption.AddValidator(validate =>
        {
            var resolutionValue = validate.GetValueOrDefault<string>()?.Trim();
            if (resolutionValue is null)
                return;

            if (_globals.ResolutionList.Contains(resolutionValue))
                return;

            Console.Error.WriteLine("Invalid resolution".Pastel(ConsoleColor.Red));
            Environment.Exit(1);
        });

        Option<int> audioBitrateInput =
            new(new[] { "-a", "-ab", "--audio-bitrate" },
                "Audio bitrate\nPossible values: 32, 64, 96, 128, 192, 256, 320");
        audioBitrateInput.SetDefaultValue(128);
        audioBitrateInput.AddValidator(validate =>
        {
            var audioBitrateValue = validate.GetValueOrDefault<int>();
            if (audioBitrateValue % 2 is 0)
                return;
            if (audioBitrateValue > 0)
                return;
            Console.Error.WriteLine("Invalid audio bitrate".Pastel(ConsoleColor.Red));
            Environment.Exit(1);
        });

        rootCommand.TreatUnmatchedTokensAsErrors = true;

        Option[] options =
        {
            randomFilenameOption,
            inputOption,
            outputOption,
            crfInput,
            resolutionOption,
            audioBitrateInput,
            keepWatermarkOption,
            sponsorBlockOption,
            videoCodecOption
        };

        foreach (var option in options)
            rootCommand.AddOption(option);

        if (args.Length is 0)
            args = new[] { "-h" };


        rootCommand.SetHandler(context => Handler(context,
            inputOption,
            outputOption,
            crfInput,
            resolutionOption,
            audioBitrateInput,
            randomFilenameOption,
            keepWatermarkOption,
            sponsorBlockOption,
            videoCodecOption));

        await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }

    private async Task Handler(InvocationContext context,
        Option<string[]> inputOption,
        Option<string> outputOption,
        Option<int> crfInput,
        Option<string> resolutionOption,
        Option<int> audioBitrateInput,
        Option<bool> randomFilenameOption,
        Option<bool> keepWatermarkOption,
        Option<bool> sponsorBlockOption,
        Option<string> videoCodecOption)
    {
        var videoUrls = context.ParseResult.GetValueForOption(inputOption)!;
        var resolution = context.ParseResult.GetValueForOption(resolutionOption);
        var crf = context.ParseResult.GetValueForOption(crfInput);
        var audioBitrate = context.ParseResult.GetValueForOption(audioBitrateInput);
        var randomFileName = context.ParseResult.GetValueForOption(randomFilenameOption);
        var keepWaterMark = context.ParseResult.GetValueForOption(keepWatermarkOption);
        var sponsorBlock = context.ParseResult.GetValueForOption(sponsorBlockOption);
        var videoCodec = context.ParseResult.GetValueForOption(videoCodecOption);
        var output = context.ParseResult.GetValueForOption(outputOption)!;

        _globals.YoutubeDl.OutputFolder = output;

        var links = videoUrls.Where(video => Uri.IsWellFormedUriString(video, UriKind.RelativeOrAbsolute)
                                             && !File.Exists(video)).ToList();
        var files = videoUrls.Where(File.Exists).ToList();

        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };

        IEnumerable<string> videoPaths = ArraySegment<string>.Empty;
        if (links.Any())
        {
            await DownloadLinks(links, parallelOptions, keepWaterMark, sponsorBlock).ConfigureAwait(false);
            videoPaths = GetVideoPaths(links);
        }

        try
        {
            foreach (var path in files.Concat(videoPaths))
            {
                await _converter.ConvertVideo(path,
                    resolution,
                    randomFileName,
                    output,
                    crf,
                    audioBitrate,
                    videoCodec).ConfigureAwait(false);
            }
        }
        finally
        {
            if (links.Any())
                Directory.Delete(_globals.TempDir, true);
        }
    }

    private async ValueTask DownloadLinks(IEnumerable<string> links,
        ParallelOptions parallelOptions,
        bool keepWaterMark,
        bool sponsorBlock)
    {
        await Parallel.ForEachAsync(links, parallelOptions, async (uri, _) =>
        {
            var (isDownloaded, _) = await _downloader.DownloadTask(uri, keepWaterMark, sponsorBlock).ConfigureAwait(false);
            if (!isDownloaded)
                _logger.Error("Failed to download: {Uri}", uri);
        }).ConfigureAwait(false);
    }

    private IEnumerable<string> GetVideoPaths(IEnumerable<string> links)
    {
        if (!links.Any())
            return new List<string>();

        var videoPaths = Directory.GetFiles(_globals.TempDir)
            .Where(file => _provider.TryGetContentType(file, out var contentType)
                           && contentType.StartsWith("video"))
            .ToList();

        if (!videoPaths.Any())
            _logger.Error("There was an error downloading the video");

        return videoPaths;
    }

    #endregion
}