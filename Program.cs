using System.CommandLine;
using System.CommandLine.Invocation;
using System.Drawing;
using System.Text;
using CliWrap;
using Microsoft.AspNetCore.StaticFiles;
using Pastel;
using Xabe.FFmpeg;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

RootCommand rootCommand = new();

YoutubeDL youtubeDl = new()
{
    FFmpegPath = "ffmpeg",
    YoutubeDLPath = "yt-dlp",
    OutputFileTemplate = "%(id)s.%(ext)s",
    OverwriteFiles = false
};

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

var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()[..4]);

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

Dictionary<string, VideoCodec> validVideoCodesMap = new()
{
    { "h264", VideoCodec.libx264},
    { "libx264", VideoCodec.libx264},
    { "h265", VideoCodec.hevc},
    { "libx265", VideoCodec.hevc},
    { "hevc", VideoCodec.hevc},
    { "vp8", VideoCodec.vp8},
    { "libvpx", VideoCodec.vp8},
    { "vp9", VideoCodec.vp9},
    { "libvpx-vp9", VideoCodec.vp9},
    { "av1", VideoCodec.av1},
    { "libaom-av1", VideoCodec.av1}
};

List<(string, VideoCodec)> validVideoExtensionsMap = new()
{
    ("mp4", VideoCodec.libx264),
    ("mp4", VideoCodec.hevc),
    ("webm", VideoCodec.vp8),
    ("webm", VideoCodec.vp9),
    ("webm", VideoCodec.av1)
};

string[] av1Args = {
    "-lag-in-frames 48",
    "-row-mt 1",
    "-tile-rows 0",
    "-tile-columns 1"
};
string[] vp9Args = {
    "-row-mt 1",
    "-lag-in-frames 25",
    "-cpu-used 4",
    "-auto-alt-ref 1",
    "-arnr-maxframes 7",
    "-arnr-strength 4",
    "-aq-mode 0",
    "-enable-tpl 1",
    "-row-mt 1",
};

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

Option<string> inputOption =
    new(new[] { "-i", "--input", "-f", "--file" },
        "A path to a video file or a link to a video");
inputOption.AddValidator(validate =>
{
    var value = validate.GetValueOrDefault<string>()?.Trim();
    if (File.Exists(value) || Uri.IsWellFormedUriString(value, UriKind.Absolute))
        return;
    Console.Error.WriteLine("File does not exist".Pastel(ConsoleColor.Red));
    Environment.Exit(1);
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
foreach (var key in validVideoCodesMap.Keys)
    videoCodecOption.AddCompletions(key);
videoCodecOption.AddValidator(validate =>
{
    var videoCodecValue = validate.GetValueOrDefault<string>()?.Trim();
    if (videoCodecValue is null)
        return;
    if (validVideoCodesMap.ContainsKey(videoCodecValue))
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
resolutionOption.AddCompletions(resolutionList);
resolutionOption.AddValidator(validate =>
{
    var resolutionValue = validate.GetValueOrDefault<string>()?.Trim();
    if (resolutionValue is null)
        return;
    if (resolutionList.Contains(resolutionValue))
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
    var input = context.ParseResult.GetValueForOption(inputOption);
    var isLink = Uri.IsWellFormedUriString(input, UriKind.Absolute);
    var output = context.ParseResult.GetValueForOption(outputOption)!;
    var resolution = context.ParseResult.GetValueForOption(resolutionOption);

    var crf = context.ParseResult.GetValueForOption(crfInput)!;
    var audioBitrate = context.ParseResult.GetValueForOption(audioBitrateInput)!;

    var randomFileName = context.ParseResult.GetValueForOption(randomFilenameOption);
    var keepWaterMark = context.ParseResult.GetValueForOption(keepWatermarkOption);
    var sponsorBlock = context.ParseResult.GetValueForOption(sponsorBlockOption);
    var videoCodecValue = context.ParseResult.GetValueForOption(videoCodecOption);

    youtubeDl.OutputFolder = output;

    if (input is null)
    {
        Console.Error.WriteLine("You must provide either a file or a link".Pastel(ConsoleColor.Red));
        return;
    }

    switch (isLink)
    {
        // If it's a local file
        case false:
            await ConvertVideo(input,
                resolution,
                randomFileName,
                output,
                crf,
                audioBitrate,
                videoCodecValue);
            break;
        // if it's an url
        case true:
            {
                var runResult = await DownloadTask(input, keepWaterMark, sponsorBlock);
                if (!runResult.Item1)
                    return;

                // get all files in tempDir of content type video using FileExtensionContentTypeProvider
                FileExtensionContentTypeProvider provider = new();
                var videoPath = Directory.GetFiles(tempDir)
                    .Where(file => provider.TryGetContentType(file, out var contentType)
                                   && contentType.StartsWith("video"))
                    .ToArray();

                if (videoPath.FirstOrDefault() is null)
                {
                    Console.Error.WriteLine("There was an error downloading the video".Pastel(ConsoleColor.Red));
                    return;
                }

                foreach (var path in videoPath)
                {
                    await ConvertVideo(path,
                        resolution,
                        randomFileName,
                        output,
                        crf,
                        audioBitrate,
                        videoCodecValue);
                }
                Directory.Delete(tempDir, true);
                break;
            }
    }
}

async Task<(bool, string? videoId)> DownloadTask(string url, bool keepWaterMarkValue, bool sponsorBlockValue)
{
    youtubeDl.OutputFolder = tempDir; // Set the output folder to the temp directory

    RunResult<VideoData> videoInfo = await youtubeDl.RunVideoDataFetch(url);
    if (!videoInfo.Success)
    {
        Console.Error.WriteLine($"{"Failed to fetch video data".Pastel(ConsoleColor.Red)}");
        return (false, null);
    }
    var videoId = videoInfo.Data.ID;

    Progress<DownloadProgress> progress = new(p =>
    {
        if (p.Progress is 0)
            return;
        Console.Write(p.DownloadSpeed != null
            ? $"\rDownload Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}\t"
            : $"\rDownload Progress: {p.Progress:P2}\t");
    });

    /*
     * Previously you could've download TikTok videos without water mark just by using the "download_addr-2".
     * But now TikTok has changed the format id to "h264_540p_randomNumber-0" so we need to get the random number
     */

    bool videoDownload;
    if (url.Contains("tiktok") && keepWaterMarkValue)
    {
        var tikTokValue = videoInfo.Data.Formats
            .Where(format => !string.IsNullOrEmpty(format.FormatId) && format.FormatId.Contains("h264_540p_"))
            .Select(format => format.FormatId.Split('_').Last().Split('-').First())
            .FirstOrDefault();

        await youtubeDl.RunVideoDownload(url,
            progress: progress,
            overrideOptions: new YoutubeDLSharp.Options.OptionSet
            {
                Format = $"h264_540p_{tikTokValue}-0"
            });
        videoDownload = true;
    }
    else if (url.Contains("youtu") && sponsorBlockValue)
    {
        await youtubeDl.RunVideoDownload(url,
            progress: progress,
            overrideOptions: new YoutubeDLSharp.Options.OptionSet
            {
                SponsorblockRemove = "all"
            });
        videoDownload = true;
    }
    else
    {
        await youtubeDl.RunVideoDownload(url, progress: progress);
        videoDownload = true;
    }

    // New line after the progress bar
    Console.WriteLine();

    if (videoDownload)
        return (true, videoId);

    Console.Error.WriteLine($"{"There was an error downloading the video".Pastel(ConsoleColor.Red)}");
    return (false, null);
}

async Task ConvertVideo(string videoFilePath,
    string? resolution,
    bool generateRandomFileName,
    string outputDirectory,
    int crf,
    int audioBitRate,
    string? videoCodec)
{
    if (!resolutionList.Contains(resolution))
        resolution = null;

    var videoCodecEnum = videoCodec is null
        ? VideoCodec.libx264
        : validVideoCodesMap[videoCodec];

    var compressedVideoPath = ReplaceVideoExtension(videoFilePath, videoCodecEnum);

    var uuid = Guid.NewGuid().ToString()[..4];
    var outputFileName = Path.GetFileName(compressedVideoPath);
    if (generateRandomFileName)
        outputFileName = $"{uuid}{Path.GetExtension(compressedVideoPath)}";

    if (File.Exists(Path.Combine(outputDirectory, outputFileName)))
        outputFileName = $"{Path.GetFileNameWithoutExtension(outputFileName)}-{uuid}{Path.GetExtension(compressedVideoPath)}";

    var outputFilePath = Path.Combine(outputDirectory, outputFileName);

    var mediaInfo = await FFmpeg.GetMediaInfo(videoFilePath);
    var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
    var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

    if (videoStream is null && audioStream is null)
    {
        Console.Error.WriteLine("There is no video or audio stream in the file");
        Environment.Exit(1);
    }

    if (videoStream != null && resolution != null)
        SetResolution(videoStream, resolution);

    var conversion = FFmpeg.Conversions.New()
        .SetPreset(ConversionPreset.VerySlow)
        .SetPixelFormat(PixelFormat.yuv420p10le)
        .AddParameter($"-crf {crf}");

    if (videoStream != null)
    {
        AddOptimizedFilter(conversion, videoStream, videoCodecEnum);
        conversion.AddStream(videoStream);
    }

    if (audioStream != null)
    {
        audioStream.SetBitrate(audioBitRate);
        audioStream.SetCodec(videoCodecEnum is VideoCodec.vp8 or VideoCodec.vp9 or VideoCodec.av1
            ? AudioCodec.libopus
            : AudioCodec.aac);
        conversion.AddStream(audioStream);
    }

    FFmpegProgressBar(conversion);
    conversion.SetOutput(outputFilePath);
    await conversion.Start();
    Console.WriteLine($"\nDone!\nConverted video saved at: {outputFilePath.Pastel("#8A2BE2")}");
}

string ReplaceVideoExtension(string videoPath, VideoCodec videoCodec)
{
    var extension = string.Empty;
    foreach (var item in validVideoExtensionsMap
                 .Where(item => item.Item2 == videoCodec))
    {
        extension = item.Item1;
        break;
    }

    return Path.ChangeExtension(videoPath, extension);
}

void SetResolution(IVideoStream videoStream, string resolution)
{
    double originalWidth = videoStream.Width;
    double originalHeight = videoStream.Height;
    var resolutionInt = int.Parse(resolution[..^1]);

    if (originalWidth > originalHeight)
    {
        var outputHeight = (int)Math.Round(originalHeight * (resolutionInt / originalWidth));
        var outputWidth = resolutionInt - resolutionInt % 2;
        outputHeight -= outputHeight % 2;
        videoStream.SetSize(outputWidth, outputHeight);
    }
    else if (originalWidth < originalHeight)
    {
        var outputWidth = (int)Math.Round(originalWidth * (resolutionInt / originalWidth));
        outputWidth -= outputWidth % 2;
        var outputHeight = resolutionInt - resolutionInt % 2;
        videoStream.SetSize(outputWidth, outputHeight);
    }
    else
    {
        var outputWidth = resolutionInt - resolutionInt % 2;
        videoStream.SetSize(outputWidth, outputWidth);
    }
}

void AddOptimizedFilter(IConversion conversion, IVideoStream videoStream, VideoCodec videoCodec)
{
    switch (videoCodec)
    {
        case VideoCodec.av1:
            {
                conversion.AddParameter(string.Join(" ", av1Args));
                {
                    switch (videoStream.Framerate)
                    {
                        case < 24:
                            conversion.AddParameter("-cpu-used 2");
                            break;
                        case > 60:
                            conversion.AddParameter("-cpu-used 6");
                            break;
                        case > 30 and < 60:
                            conversion.AddParameter("-cpu-used 4");
                            break;
                    }
                    break;
                }
            }
        case VideoCodec.vp9:
            conversion.AddParameter(string.Join(" ", vp9Args));
            break;
    }
    videoStream.SetCodec(videoCodec);
}

void FFmpegProgressBar(IConversion conversion)
{
    conversion.OnProgress += (_, args) =>
    {
        const string startHex = "#FFC0CB";
        const string endHex = "#8A2BE2";
        var percent = args.Duration.TotalSeconds / args.TotalLength.TotalSeconds;
        var eta = args.TotalLength - args.Duration;
        var progress = (int)Math.Round(percent * 100);
        StringBuilder progressString = new();
        for (var i = 0; i < 100; i++)
        {
            var color = CalculateColor(startHex, endHex, i, progress);
            if (i < progress)
                progressString.Append($"{'█'}".Pastel(color));
            else if (i == progress)
                progressString.Append($"{'▓'}".Pastel(color));
            else
                progressString.Append('░');
        }
        Console.Write($"\rProgress: {progressString} {progress}% | ETA: {eta:hh\\:mm\\:ss}");
    };
}

static Color CalculateColor(string startHex, string endHex, int i, int progress)
{
    /*
     * The values of red, green, and blue are calculated based on the progress of the task and the start and end colors.
     * For each iteration, the values of red, green, and blue are determined by a weighted average of the start and end color values.
     * The weight of the start color decreases as the progress increases, while the weight of the end color increases.
     * The formula for each component (red, green, or blue) is as follows:
     * component = (1 - i / 100) * startColor.component + (i / 100) * endColor.component
     * where component is either R, G, or B depending on the component being calculated
     * and i is the current iteration, which ranges from 0 to 100.
    */
    var startColor = ColorTranslator.FromHtml(startHex);
    var endColor = ColorTranslator.FromHtml(endHex);
    var weight = (double)i / 100;
    if (i > progress)
        weight = 1 - weight;
    var red = (int)((1.0 - weight) * startColor.R + weight * endColor.R);
    var green = (int)((1.0 - weight) * startColor.G + weight * endColor.G);
    var blue = (int)((1.0 - weight) * startColor.B + weight * endColor.B);
    return Color.FromArgb(red, green, blue);
}