using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Xabe.FFmpeg;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

var tempDir = Path.GetTempPath();

RootCommand rootCommand = new();

YoutubeDL youtubeDl = new()
{
    FFmpegPath = "ffmpeg",
    YoutubeDLPath = "yt-dlp",
    OutputFileTemplate = "%(id)s.%(ext)s",
    OverwriteFiles = false
};

Dictionary<string, (int width, int height)> resolutionMap = new()
{
    ["144p"] = (256, 144),
    ["240p"] = (426, 240),
    ["360p"] = (640, 360),
    ["480p"] = (854, 480),
    ["720p"] = (1280, 720),
    ["1080p"] = (1920, 1080),
    ["1440p"] = (2560, 1440),
    ["2160p"] = (3840, 2160)
};

try
{
    using var ffmpeg = Process.Start(new ProcessStartInfo("ffmpeg", "-version")
    {
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    });
}
catch (Exception)
{
    Console.Error.WriteLine("ffmpeg is not installed");
    return;
}
try
{
    using var ytDlp = Process.Start(new ProcessStartInfo("yt-dlp", "--version")
    {
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    });
    ytDlp?.WaitForExit();
}
catch (Exception)
{
    Console.Error.WriteLine("yt-dlp is not installed");
    return;
}

System.CommandLine.Option<bool> randomFilename =
    new (new[] { "-rn", "-rd", "-rnd", "--random" },
        "Randomize the filename");
System.CommandLine.Option<bool> keepWatermark =
    new(new[] { "-k", "-kw", "-kwm", "--keep" },
        "Keep the watermark");
System.CommandLine.Option<bool> sponsorBlock =
    new(new[] { "-sb", "-sponsorblock", "--sponsorblock" },
        "Remove the sponsorblock from the video");
keepWatermark.SetDefaultValue(false);

System.CommandLine.Option<string> fileInput =
    new(new[] { "-i", "--input" }, "A path to a video file");
fileInput.AddValidator(validate =>
{
    var file = validate.GetValueOrDefault<string>();
    if (File.Exists(file))
        return;
    Console.Error.WriteLine("File does not exist");
    Environment.Exit(1);
});

System.CommandLine.Option<string> linkInput =
    new(new[] { "-l", "--link" }, "A URL link to a video");
linkInput.AddValidator(validate =>
{
    if (validate.Tokens.Any(token
            => Uri.IsWellFormedUriString(token.Value, UriKind.RelativeOrAbsolute)))
        return;
    Console.Error.WriteLine("Invalid URL");
    Environment.Exit(1);
});

System.CommandLine.Option<string> output =
    new(new[] { "-o", "--output" },
        "Directory to save the compressed video to\n");
output.SetDefaultValue(Environment.CurrentDirectory);
output.AddValidator(validate =>
{
    var outputValue = validate.GetValueOrDefault<string>();
    if (Directory.Exists(outputValue))
        return;
    Console.Error.WriteLine("Output directory does not exist");
    Environment.Exit(1);
});

System.CommandLine.Option<int> crfInput =
    new(new[] { "-c", "--crf" },
        "CRF value");
crfInput.SetDefaultValue(29);
crfInput.AddValidator(validate =>
{
    var crfValue = validate.GetValueOrDefault<int>();
    if (crfValue is >= 0 and <= 51)
        return;
    Console.Error.WriteLine("CRF value must be between 0 and 51");
    Environment.Exit(1);
});

System.CommandLine.Option<string> resolutionInput =
    new(new[] { "-r", "--resolution" },
        "Resolution");
resolutionInput.AddCompletions("144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p");
resolutionInput.AddValidator(validate =>
{
    var resolutionValue = validate.GetValueOrDefault<string>();
    if (resolutionMap.ContainsKey(resolutionValue!) ||
        resolutionMap.ContainsKey($"{resolutionValue}p"))
        return;
    Console.Error.WriteLine("Invalid resolution");
    Environment.Exit(1);
});

System.CommandLine.Option<string> audioBitrateInput =
    new(new[] { "-a", "-ab", "--audio-bitrate" },
        "Audio bitrate");
audioBitrateInput.SetDefaultValue("128k");
audioBitrateInput.AddCompletions("32k", "64k", "96k", "128k", "192k", "256k", "320k");
audioBitrateInput.AddValidator(validate =>
{
    var audioBitrateValue = validate.GetValueOrDefault<string>()!;
    var audioBitrates = new[]
    {
        "32k", "64k", "96k", "128k", "192k", "256k", "320k"
    };
    if (audioBitrates.Any(audioBitrateValue.Contains))
        return;
    Console.Error.WriteLine("Invalid audio bitrate");
    Environment.Exit(1);
});

rootCommand.TreatUnmatchedTokensAsErrors = true;

foreach (var option in new Option[]
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
         }) rootCommand.AddOption(option);

if (args.Length is 0)
    args = new[] { "-h" };

rootCommand.SetHandler(HandleInput);

await rootCommand.InvokeAsync(args);

async Task HandleInput(InvocationContext invocationContext)
{
    var fileValue = invocationContext.ParseResult.GetValueForOption(fileInput);
    var linkValue = invocationContext.ParseResult.GetValueForOption(linkInput);
    var outputValue = invocationContext.ParseResult.GetValueForOption(output)!;
    var crfValue = invocationContext.ParseResult.GetValueForOption(crfInput)!;

    var resolutionValue = invocationContext.ParseResult.GetValueForOption(resolutionInput);

    var audioBitrateValue = invocationContext.ParseResult.GetValueForOption(audioBitrateInput);
    var randomFilenameValue = invocationContext.ParseResult.GetValueForOption(randomFilename);
    var keepWaterMarkValue = invocationContext.ParseResult.GetValueForOption(keepWatermark);
    var sponsorBlockValue = invocationContext.ParseResult.GetValueForOption(sponsorBlock);

    youtubeDl.OutputFolder = outputValue;

    if (fileValue is null && linkValue is null)
    {
        Console.WriteLine("You must provide either a file or a link");
        return;
    }

    // if it's a local file
    if (linkValue is null && fileValue is not null)
        await ConvertVideoTask(fileValue);

    // if it's an url
    if (fileValue is null && linkValue is not null)
    {
        youtubeDl.OutputFolder = tempDir; // Set the output folder to the temp directory

        var videoInfo = await youtubeDl.RunVideoDataFetch(linkValue);
        if (!videoInfo.Success)
        {
            Console.Error.WriteLine("Failed to fetch video data");
            return;
        }
        var videoId = videoInfo.Data.ID;

        Progress<DownloadProgress> progress = new(p =>
        {
            if (p.Progress is 0)
                return;
            Console.Write($"Download Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}\t\r");
        });

        var videoDownload = linkValue switch
        {
            _ when linkValue.Contains("tiktok.com") && keepWaterMarkValue => await youtubeDl.RunVideoDownload(linkValue,
                progress: progress,
                overrideOptions: new OptionSet
                {
                    Format = "download_addr-2"
                }),
            _ when linkValue.Contains("yout") && sponsorBlockValue => await youtubeDl.RunVideoDownload(linkValue,
                progress: progress,
                overrideOptions: new OptionSet
                {
                    SponsorblockRemove = "all"
                }),
            _ => await youtubeDl.RunVideoDownload(linkValue, progress: progress)
        };
        // New line after the progress bar
        Console.WriteLine();

        if (!videoDownload.Success)
        {
            Console.Error.WriteLine("There was an error downloading the video");
            return;
        }

        var videoPath = Path.Combine(tempDir, $"{videoId}.{videoInfo.Data?.Extension}");
        await ConvertVideoTask(videoPath);
    }

    async Task ConvertVideoTask(string videoPath)
    {
        if (!new[] { "144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p" }.Contains(resolutionValue))
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

        if (File.Exists(videoPath) && isLocalFIle)
            outputFilename = $"{Path.GetFileNameWithoutExtension(outputFilename)}-{uuid}.mp4";

        var videoPathConverted = Path.Combine(outputValue, outputFilename);

        // get video resolution
        var video = await FFmpeg.GetMediaInfo(videoPath);

        // Get Video Stream Width and Height
        var videoStream = video.VideoStreams.ToList();
        var audioStream = video.AudioStreams.ToList();
        var originalWidth = videoStream.First().Width;
        var originalHeight = videoStream.First().Height;

        var outputHeight = 0;
        var outputWidth = 0;

        if (resolutionChange)
        {
            outputWidth = resolutionMap[resolutionValue!].width;
            outputHeight = resolutionMap[resolutionValue!].height;

            switch (Math.Sign(originalWidth - originalHeight))
            {
                case 1:
                    // If the input video is landscape orientation, use the full width and adjust the height
                    outputHeight = (int)Math.Round(originalHeight * ((double)outputWidth / originalWidth));
                    // Round down the width and height to the nearest multiple of 2
                    outputWidth -= outputWidth % 2;
                    outputHeight -= outputHeight % 2;
                    break;
                case -1:
                    // If the input video is portrait orientation, use the full height and adjust the width
                    outputWidth = (int)Math.Round(originalWidth * ((double)outputHeight / originalHeight));
                    // Round down the width and height to the nearest multiple of 2
                    outputWidth -= outputWidth % 2;
                    outputHeight -= outputHeight % 2;
                    break;
                default:
                    // If the input video is square, use the full width and height of the selected resolution
                    outputWidth = resolutionMap[resolutionValue!].width;
                    outputHeight = resolutionMap[resolutionValue!].height;
                    break;
            }

            videoStream.First().SetSize(outputWidth, outputHeight);
        }

        var parameter = $"-c:v libx264 -crf {crfValue}";
        parameter += resolutionChange
            ? $" -vf scale=h={outputHeight}:w={outputWidth} -pix_fmt yuv420p -c:a aac -b:a {audioBitrateValue}"
            : $" -pix_fmt yuv420p -c:a aac -b:a {audioBitrateValue}";

        var conversion = FFmpeg.Conversions.New()
            .AddStream(videoStream)
            .AddStream(audioStream)
            .AddParameter(parameter)
            .SetOutput(videoPathConverted);

        conversion.OnProgress += (_, args) =>
        {
            var percent = args.Duration.TotalSeconds / args.TotalLength.TotalSeconds;
            var eta = args.TotalLength - args.Duration;
            Console.Write($"\rCompressing Progress: {percent:P2} | ETA: {eta}");
        };

        await conversion.Start();
        Console.WriteLine($"\nDone!\nConverted video saved at {outputFilename}");
        if(!isLocalFIle)
            File.Delete(videoPath);
    }
}