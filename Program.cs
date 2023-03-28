using System.CommandLine;
using System.CommandLine.Invocation;
using CliWrap;
using dis;
using Microsoft.AspNetCore.StaticFiles;
using Pastel;
using static dis.Globals;

RootCommand rootCommand = new();

try
{
    await Cli.Wrap("ffmpeg")
        .WithArguments("-version")
        .WithValidation(CommandResultValidation.None)
        .ExecuteAsync();
}
catch (Exception)
{
    Console.Error.WriteLine("FFmpeg is not installed".Pastel(ConsoleColor.Red));
    Environment.Exit(1);
}
try
{
    await Cli.Wrap("yt-dlp")
        .WithArguments("--version")
        .WithValidation(CommandResultValidation.None)
        .ExecuteAsync();
}
catch (Exception)
{
    Console.Error.WriteLine("yt-dlp is not installed".Pastel(ConsoleColor.Red));
    Environment.Exit(1);
}

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
    { AllowMultipleArgumentsPerToken = true };
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
foreach (var key in ValidVideoCodesMap.Keys)
    videoCodecOption.AddCompletions(key);
videoCodecOption.AddValidator(validate =>
{
    var videoCodecValue = validate.GetValueOrDefault<string>()?.Trim();
    if (videoCodecValue is null)
        return;
    if (ValidVideoCodesMap.ContainsKey(videoCodecValue))
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
resolutionOption.AddCompletions(ResolutionList);
resolutionOption.AddValidator(validate =>
{
    var resolutionValue = validate.GetValueOrDefault<string>()?.Trim();
    if (resolutionValue is null)
        return;

    if (ResolutionList.Contains(resolutionValue))
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

rootCommand.SetHandler(Handler);

await rootCommand.InvokeAsync(args);

async Task Handler(InvocationContext context)
{
    var videoUrls = context.ParseResult.GetValueForOption(inputOption)!;
    var resolution = context.ParseResult.GetValueForOption(resolutionOption);

    var crf = context.ParseResult.GetValueForOption(crfInput)!;
    var audioBitrate = context.ParseResult.GetValueForOption(audioBitrateInput)!;

    var randomFileName = context.ParseResult.GetValueForOption(randomFilenameOption);
    var keepWaterMark = context.ParseResult.GetValueForOption(keepWatermarkOption);
    var sponsorBlock = context.ParseResult.GetValueForOption(sponsorBlockOption);

    var videoCodec = context.ParseResult.GetValueForOption(videoCodecOption);
    var output = context.ParseResult.GetValueForOption(outputOption)!;
    YoutubeDl.OutputFolder = output;

    var links = videoUrls.Where(video => Uri.IsWellFormedUriString(video, UriKind.RelativeOrAbsolute) && !File.Exists(video)).ToList();
    var files = videoUrls.Where(File.Exists).ToList();

    ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };

    await Parallel.ForEachAsync(links, parallelOptions, async (uri, _) =>
    {
        var (isDownloaded, _) = await Downloader.DownloadTask(uri, keepWaterMark, sponsorBlock).ConfigureAwait(false);
        if (!isDownloaded)
            Console.Error.WriteLine($"Failed to download: {uri}".Pastel(ConsoleColor.Red));
    }).ConfigureAwait(false);

    FileExtensionContentTypeProvider provider = new();

    var videoPaths = Directory.GetFiles(TempDir)
        .Where(file => provider.TryGetContentType(file, out var contentType)
                       && contentType.StartsWith("video"))
        .ToArray();

    if (videoPaths.FirstOrDefault() is null)
    {
        Console.Error.WriteLine("There was an error downloading the video".Pastel(ConsoleColor.Red));
        return;
    }

    try
    {
        foreach (var path in files.Concat(videoPaths))
        {
            await Converter.ConvertVideo(path,
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
        Directory.Delete(TempDir, true);
    }
}