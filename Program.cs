using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using CliWrap;
using Microsoft.AspNetCore.StaticFiles;
using Pastel;
using Xabe.FFmpeg;
using YoutubeDLSharp;

RootCommand rootCommand = new();

YoutubeDL youtubeDl = new()
{
    FFmpegPath = "ffmpeg",
    YoutubeDLPath = "yt-dlp",
    OutputFileTemplate = "%(id)s.%(ext)s",
    OverwriteFiles = false
};

var tempDir = Path.GetTempPath();

string[] resolutionList =
{
    "144p",
    "240p",
    "360p",
    "480p",
    "720p",
    "1080p",
    "1440p",
    "2160p"
};

string[] audioBitRates = { "32k", "64k", "96k", "128k", "192k", "256k", "320k" };

var ffmpeg = await Cli.Wrap("ffmpeg")
    .WithArguments("-version")
    .WithValidation(CommandResultValidation.ZeroExitCode)
    .ExecuteAsync();
if (ffmpeg.ExitCode is not 0)
{
    Console.Error.WriteLine($"{"FFmpeg is not installed".Pastel(ConsoleColor.Red)}");
    Environment.Exit(1);
}
var ytDlp = await Cli.Wrap("yt-dlp")
    .WithArguments("--version")
    .WithValidation(CommandResultValidation.None)
    .ExecuteAsync();
if (ytDlp.ExitCode is not 120)
{
    Console.Error.WriteLine($"{"yt-dlp is not installed".Pastel(ConsoleColor.Red)}");
    Environment.Exit(1);
}

Option<bool> randomFilename =
    new(new[] { "-rn", "-rd", "-rnd", "--random" },
        "Randomize the filename");
Option<bool> keepWatermark =
    new(new[] { "-k", "-kw", "-kwm", "--keep" },
        "Keep the watermark");
Option<bool> sponsorBlock =
    new(new[] { "-sb", "-sponsorblock", "--sponsorblock" },
        "Remove the sponsorblock from the video");
keepWatermark.SetDefaultValue(false);

Option<string> fileInput =
    new(new[] { "-i", "--input" }, "A path to a video file");
fileInput.AddValidator(validate =>
{
    var file = validate.GetValueOrDefault<string>();
    if (File.Exists(file))
        return;
    Console.Error.WriteLine("File does not exist");
    Environment.Exit(1);
});

Option<string> linkInput =
    new(new[] { "-l", "--link" }, "A URL link to a video");
linkInput.AddValidator(validate =>
{
    if (validate.Tokens.Any(token
            => Uri.IsWellFormedUriString(token.Value, UriKind.RelativeOrAbsolute)))
        return;
    Console.Error.WriteLine("Invalid URL");
    Environment.Exit(1);
});

Option<string> output =
    new(new[] { "-o", "--output" },
        "Directory to save the compressed video to\n");
output.SetDefaultValue(Environment.CurrentDirectory);
output.AddValidator(validate =>
{
    var outputValue = validate.GetValueOrDefault<string>();
    if (Directory.Exists(outputValue))
        return;
    Console.Error.WriteLine($"{"Output directory does not exist".Pastel(ConsoleColor.Red)}");
    Environment.Exit(1);
});

Option<int> crfInput =
    new(new[] { "-c", "--crf" },
        "CRF value");
crfInput.SetDefaultValue(29);
crfInput.AddValidator(validate =>
{
    var crfValue = validate.GetValueOrDefault<int>();
    if (crfValue is >= 0 and <= 51)
        return;
    Console.Error.WriteLine($"{"CRF value must be between 0 and 51".Pastel(ConsoleColor.Red)}");
    Environment.Exit(1);
});

Option<string> resolutionInput =
    new(new[] { "-r", "--resolution" },
        "Resolution");
resolutionInput.AddCompletions("144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p");
resolutionInput.AddValidator(validate =>
{
    var resolutionValue = validate.GetValueOrDefault<string>();
    if (resolutionValue is null)
        return;
    if (resolutionList.Contains(resolutionValue))
        return;

    Console.Error.WriteLine($"{"Invalid resolution".Pastel(ConsoleColor.Red)}");
    Environment.Exit(1);
});

Option<string> audioBitrateInput =
    new(new[] { "-a", "-ab", "--audio-bitrate" },
        "Audio bitrate");
audioBitrateInput.SetDefaultValue("128k");
audioBitrateInput.AddCompletions("32k", "64k", "96k", "128k", "192k", "256k", "320k");
audioBitrateInput.AddValidator(validate =>
{
    var audioBitrateValue = validate.GetValueOrDefault<string>()!;
    if (audioBitRates.Any(audioBitrateValue.Contains))
        return;
    Console.Error.WriteLine($"{"Invalid audio bitrate".Pastel(ConsoleColor.Red)}");
    Environment.Exit(1);
});

rootCommand.TreatUnmatchedTokensAsErrors = true;

Option[] options =
{
    randomFilename,
    fileInput,
    linkInput,
    output,
    crfInput,
    resolutionInput,
    audioBitrateInput,
    keepWatermark,
    sponsorBlock
};

foreach (var option in options)
    rootCommand.AddOption(option);

if (args.Length is 0)
    args = new[] { "-h" };

rootCommand.SetHandler(HandleInput);

await rootCommand.InvokeAsync(args);

async Task HandleInput(InvocationContext invocationContext)
{
    var fileValue = invocationContext.ParseResult.GetValueForOption(fileInput);
    var linkValue = invocationContext.ParseResult.GetValueForOption(linkInput);
    var outputValue = invocationContext.ParseResult.GetValueForOption(output)!;
    var resolutionValue = invocationContext.ParseResult.GetValueForOption(resolutionInput);

    var crfValue = invocationContext.ParseResult.GetValueForOption(crfInput)!;
    var audioBitrateValue = int.Parse(invocationContext.ParseResult.GetValueForOption(audioBitrateInput)!.Replace("k", string.Empty));

    var randomFilenameValue = invocationContext.ParseResult.GetValueForOption(randomFilename);
    var keepWaterMarkValue = invocationContext.ParseResult.GetValueForOption(keepWatermark);
    var sponsorBlockValue = invocationContext.ParseResult.GetValueForOption(sponsorBlock);

    youtubeDl.OutputFolder = outputValue;

    if (fileValue is null && linkValue is null)
    {
        Console.Error.WriteLine($"{"You must provide either a file or a link".Pastel(ConsoleColor.Red)}");
        return;
    }

    // if it's a local file
    if (linkValue is null && fileValue is not null)
        await ConvertVideoTask(fileValue, resolutionValue, fileValue, randomFilenameValue, outputValue, crfValue, audioBitrateValue);

    // if it's an url
    if (fileValue is null && linkValue is not null)
    {
        var runResult = await DownloadTask(linkValue, keepWaterMarkValue, sponsorBlockValue);
        if (!runResult.Item1)
            return;

        var videoPath = Directory.GetFiles(tempDir, $"{runResult.videoId}*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(x => new FileExtensionContentTypeProvider()
                .TryGetContentType(x, out var contentType) && contentType.StartsWith("video"));
        if (videoPath is null)
        {
            Console.Error.WriteLine($"{"There was an error downloading the video".Pastel(ConsoleColor.Red)}");
            return;
        }

        await ConvertVideoTask(videoPath, resolutionValue, null, randomFilenameValue, outputValue, crfValue, audioBitrateValue);
    }
}

async Task<(bool, string? videoId)> DownloadTask(string url, bool keepWaterMarkValue, bool sponsorBlockValue)
{
    youtubeDl.OutputFolder = tempDir; // Set the output folder to the temp directory

    var videoInfo = await youtubeDl.RunVideoDataFetch(url);
    if (!videoInfo.Success)
    {
        Console.Error.WriteLine($"{"Failed to fetch video data".Pastel(ConsoleColor.Red)}");
        return (false,null);
    }
    var videoId = videoInfo.Data.ID;

    Progress<DownloadProgress> progress = new(p =>
    {
        if (p.Progress is 0)
            return;
        Console.Write($"Download Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}\t\r");
    });

    var videoDownload = url switch
    {
        _ when url.Contains("tiktok.com") && keepWaterMarkValue => await youtubeDl.RunVideoDownload(url,
            progress: progress,
            overrideOptions: new YoutubeDLSharp.Options.OptionSet
            {
                Format = "download_addr-2"
            }),
        _ when url.Contains("youtu") && sponsorBlockValue => await youtubeDl.RunVideoDownload(url,
            progress: progress,
            overrideOptions: new YoutubeDLSharp.Options.OptionSet
            {
                SponsorblockRemove = "all"
            }),
        _ => await youtubeDl.RunVideoDownload(url, progress: progress)
    };
    // New line after the progress bar
    Console.WriteLine();

    if (videoDownload.Success)
        return (true,videoId);

    Console.Error.WriteLine($"{"There was an error downloading the video".Pastel(ConsoleColor.Red)}");
    return (false,null);
}

async Task ConvertVideoTask(string videoPath,
    string? resolutionValue,
    string? fileValue,
    bool randomFilenameValue,
    string outputValue,
    int crfValue,
    int audioBitrateValue)
{
    if (resolutionList.Contains(resolutionValue))
        resolutionValue = null;

    var resolutionChange = resolutionValue is not null;
    var isLocalFIle = fileValue is not null;

    var uuid = Guid.NewGuid().ToString()[..4];

    /*
     * Extract the filename or video id depending on the input provided by the user.
     * Generate a random filename if needed.
     * Modify the filename by replacing the extension.
     * Set the output path to the output folder plus the modified filename.
     */

    var outputFilename = Path.GetFileName(videoPath);

    if (randomFilenameValue)
        outputFilename = $"{uuid}.mp4";
    else
        outputFilename = $"{Path.GetFileNameWithoutExtension(outputFilename)}.mp4";

    if (File.Exists(videoPath) && isLocalFIle)
        outputFilename = $"{Path.GetFileNameWithoutExtension(outputFilename)}-{uuid}.mp4";

    var videoPathConverted = Path.Combine(outputValue, outputFilename);

    // get video resolution
    var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);

    // Get Video Stream Width and Height
    var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
    var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
    if (videoStream is null)
        resolutionChange = false;

    double originalWidth = 0;
    double originalHeight = 0;
    if (videoStream is not null)
    {
        originalWidth = videoStream.Width;
        originalHeight = videoStream.Height;
    }

    if (resolutionChange)
    {
        var resolutionMap = resolutionList.ToDictionary(x => x.ToString(),
            x => (Width: int.Parse(x.ToString()[..^1]), Height: int.Parse(x.ToString()[..^1])));
        var outputWidth = resolutionMap[resolutionValue!].Width;
        var outputHeight = resolutionMap[resolutionValue!].Height;

        switch (Math.Sign(originalWidth - originalHeight))
        {
            case 1:
                // If the input video is landscape orientation, use the full width and adjust the height
                outputHeight = (int)Math.Round(originalHeight * (outputWidth / originalWidth));
                // Round down the width and height to the nearest multiple of 2
                outputWidth -= outputWidth % 2;
                outputHeight -= outputHeight % 2;
                break;
            case -1:
                // If the input video is portrait orientation, use the full height and adjust the width
                outputWidth = (int)Math.Round(originalWidth * (outputHeight / originalHeight));
                // Round down the width and height to the nearest multiple of 2
                outputWidth -= outputWidth % 2;
                outputHeight -= outputHeight % 2;
                break;
            default:
                // If the input video is square, use the full width and height of the selected resolution
                outputWidth = resolutionMap[resolutionValue!].Width;
                outputHeight = resolutionMap[resolutionValue!].Height;
                break;
        }

        videoStream!.SetSize(outputWidth, outputHeight);
    }

    var conversion = FFmpeg.Conversions.New()
        .SetPreset(ConversionPreset.VerySlow)
        .SetPixelFormat(PixelFormat.yuv420p)
        .AddParameter($"-crf {crfValue}")
        .SetOutput(videoPathConverted);

    if (videoStream is not null)
    {
        videoStream.SetCodec(VideoCodec.h264);
        conversion.AddStream(videoStream);
    }
    if (audioStream is not null)
    {
        audioStream.SetBitrate(Convert.ToInt64(audioBitrateValue));
        audioStream.SetCodec(AudioCodec.aac);
        conversion.AddStream(audioStream);
    }

    conversion.OnProgress += (_, args) =>
    {
        var percent = args.Duration.TotalSeconds / args.TotalLength.TotalSeconds;
        var eta = args.TotalLength - args.Duration;
        var progress = (int)Math.Round(percent * 100);
        var progressString = new StringBuilder();
        for (var i = 0; i < 100; i++)
        {
            if (i < progress)
                progressString.Append('█');
            else if (i == progress)
                progressString.Append('▓');
            else
                progressString.Append('░');
        }
        Console.Write($"\rCompression Progress: {progressString} {percent:P2} | ETA: {eta:mm\\:ss}\t");
    };

    if (File.Exists(videoPathConverted))
    {
        Console.WriteLine($"{"File already exists!".Pastel(ConsoleColor.Red)}");
        Console.WriteLine($"{"Do you want to overwrite it? (y/n)".Pastel(ConsoleColor.DarkYellow)}");
        var key = Console.ReadKey();
        if (key.Key is not ConsoleKey.Y)
            return;

        File.Delete(videoPathConverted);
        Console.WriteLine();
    }
    
    await conversion.Start();
    Console.WriteLine(
        $"\n{"Done!".Pastel(ConsoleColor.Green)}\n" +
        $"Converted video saved at: {videoPathConverted}");
    if (!isLocalFIle)
        File.Delete(videoPath);
}