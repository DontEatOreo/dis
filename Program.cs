using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

var tempDir = Path.GetTempPath();
var separator = Path.DirectorySeparatorChar;

RootCommand rootCommand = new();

YoutubeDL youtubeDl = new()
{
    FFmpegPath = "ffmpeg",
    YoutubeDLPath = "yt-dlp",
    OutputFileTemplate = "%(id)s.%(ext)s",
    OverwriteFiles = false
};

Regex twitterRegex = new(@"^[1][4-5][0-9]{18}$", RegexOptions.Compiled);
Regex youtubeRegex = new(@"^([a-zA-Z0-9_-]{11})$", RegexOptions.Compiled);
Regex redditRegex = new(@"^[xy][a-zA-Z0-9]{5}$", RegexOptions.Compiled);
Regex tikTokRegex = new(@"^[67][0-9]{18}$", RegexOptions.Compiled);

Dictionary<Regex, string> urlRegex = new()
{
    { twitterRegex, "https:/twitter.com/i/status/" },
    { youtubeRegex, "https://youtu.be/" },
    { redditRegex, "https://reddit.com/" },
    { tikTokRegex, "https://www.tiktok.com/@dis/video/" }
};

System.CommandLine.Option<bool> randomFilename = new(new[] { "-rn", "-rd", "-rnd", "--random" }, "Randomize the filename");
System.CommandLine.Option<bool> deleteOriginal = new(new[] { "-d", "-dl", "-del", "--delete" }, "Delete the original file");
System.CommandLine.Option<bool> keepWatermark = new(new[] { "-k", "-kw", "-kwm", "--keep" }, "Keep the watermark");
System.CommandLine.Option<bool> sponsorBlock = new(new[] { "-sb", "-sponsorblock", "--sponsorblock" }, "Remove the sponsorblock from the video");

System.CommandLine.Option<string> fileInput = new(new[] { "-i", "--input" }, "A path to a video file");
fileInput.AddValidator(validate =>
{
    var file = validate.GetValueOrDefault<string>();
    if (File.Exists(file)) return;
    Console.WriteLine("File does not exist");
    Environment.Exit(1);
});

System.CommandLine.Option<string> linkInput = new(new[] { "-l", "--link" }, "A URL link to a video");
linkInput.AddValidator(validate =>
{
    if (urlRegex.Any(x => x.Key.IsMatch(validate.Token?.Value!)) ||
        Uri.IsWellFormedUriString(validate.Tokens[0].Value, UriKind.RelativeOrAbsolute)) return;
    Console.WriteLine("Invalid URL");
    Environment.Exit(1);
});

System.CommandLine.Option<string> output = new(new[] { "-o", "--output" }, "Directory to save the compressed video to\n");
output.SetDefaultValue(Environment.CurrentDirectory);
output.AddValidator(validate =>
{
    var outputValue = validate.GetValueOrDefault<string>();
    if (Directory.Exists(outputValue)) return;
    Console.WriteLine("Output directory does not exist");
    Environment.Exit(1);
});

System.CommandLine.Option<int> crfInput = new(new[] { "-c", "--crf" }, "CRF value");
crfInput.SetDefaultValue(26);
crfInput.AddValidator(validate =>
{
    var crfValue = validate.GetValueOrDefault<int>();
    if (crfValue is >= 0 and <= 51) return;
    Console.WriteLine("CRF value must be between 0 and 51");
    Environment.Exit(1);
});

System.CommandLine.Option<string> resolutionInput = new(new[] { "-r", "--resolution" }, "Resolution");
resolutionInput.AddCompletions("144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p");
resolutionInput.AddValidator(validate =>
{
    var resolutionValue = validate.GetValueOrDefault<string>();
    var resolutions = new[] { "144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p" };
    if (resolutions.Any(x => x.Contains(resolutionValue!)) 
        || resolutions.Contains(resolutionValue + "p")) return;
    Console.WriteLine("Invalid resolution");
    Environment.Exit(1);
});

System.CommandLine.Option<string> audioBitrateInput = new(new[] { "-a", "-ab", "--audio-bitrate" }, "Audio bitrate");
audioBitrateInput.SetDefaultValue("128");
audioBitrateInput.AddCompletions("32", "64", "96", "128", "192", "256", "320");
audioBitrateInput.AddValidator(validate =>
{
    var audioBitrateValue = validate.GetValueOrDefault<string>();
    var audioBitrates = new[] { "32", "64", "96", "128", "192", "256", "320" };
    if (audioBitrateValue!.EndsWith("k")) audioBitrateValue = audioBitrateValue[..^1];
    if (audioBitrates.Contains(audioBitrateValue)) return;
    Console.WriteLine("Invalid audio bitrate");
    Environment.Exit(1);
});

rootCommand.TreatUnmatchedTokensAsErrors = true;

foreach (var option in new Option[]
         {
             randomFilename,
             deleteOriginal,
             fileInput,
             linkInput,
             output,
             crfInput,
             resolutionInput,
             audioBitrateInput,
             keepWatermark,
             sponsorBlock
         }) rootCommand.AddOption(option);

if (args.Length is 0) args = new[] { "-h" };

rootCommand.SetHandler(HandleInput);

await rootCommand.InvokeAsync(args);

async Task HandleInput(InvocationContext invocationContext)
{
    var fileValue = invocationContext.ParseResult.GetValueForOption(fileInput);
    var linkValue = invocationContext.ParseResult.GetValueForOption(linkInput);
    var outputValue = invocationContext.ParseResult.GetValueForOption(output);
    var crfValue = invocationContext.ParseResult.GetValueForOption(crfInput);

    var resolutionValue = invocationContext.ParseResult.GetValueForOption(resolutionInput);
    if (resolutionValue is not null && !resolutionValue.EndsWith("p")) resolutionValue += "p";

    var audioBitrateValue = invocationContext.ParseResult.GetValueForOption(audioBitrateInput);
    var randomFilenameValue = invocationContext.ParseResult.GetValueForOption(randomFilename);
    var deleteOriginalValue = invocationContext.ParseResult.GetValueForOption(deleteOriginal);
    var keepWaterMarkValue = invocationContext.ParseResult.GetValueForOption(keepWatermark);
    var sponsorBlockValue = invocationContext.ParseResult.GetValueForOption(sponsorBlock);

    youtubeDl.OutputFolder = outputValue!;
    
    if (fileValue is null && linkValue is null)
    {
        Console.WriteLine("You must provide either a file or a link");
        return;
    }

    if (fileValue is not null && linkValue is null)
    {
        var fileName = Path.GetFileNameWithoutExtension(fileValue);
        await ConvertVideoTask(fileValue, fileName);
    }

    if (fileValue is null && linkValue is not null)
    {
        youtubeDl.OutputFolder = tempDir; // Set the output folder to the temp directory
        
        // We loop through the regexes if the link is a video id in-order to construct a full URL
        foreach (var (regex, url) in urlRegex)
            if (regex.IsMatch(linkValue))
                linkValue = url + linkValue;

        RunResult<VideoData?> videoInfo;
        try
        {
            videoInfo = await youtubeDl.RunVideoDataFetch(linkValue);
        }
        catch (Exception)
        {
            Console.WriteLine("Invalid link");
            return;
        }
        var videoId = videoInfo.Data?.ID;
        
        var progress = new Progress<DownloadProgress>(p => 
        {
            if (p.Progress is 0) return;
            Console.Write($"Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}\t\r");
        });
        
        RunResult<string?> videoDownload;
        try
        {
            videoDownload = linkValue switch
            {
                var l when l.Contains("tiktok.com") && keepWaterMarkValue => await youtubeDl.RunVideoDownload(linkValue,
                    progress: progress,
                    overrideOptions: new OptionSet
                    {
                        Format = "download_addr-2"
                    }),
                var l when l.Contains("youtu.be") && sponsorBlockValue => await youtubeDl.RunVideoDownload(linkValue,
                    progress: progress,  
                    overrideOptions: new OptionSet
                    {
                        SponsorblockRemove = "all"
                    }),
                _ => await youtubeDl.RunVideoDownload(linkValue, progress: progress)
            };
        }
        catch (Exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Could not download the video");
            Console.ResetColor();
            return;
        }
        Console.WriteLine();

        if (!videoDownload.Success)
        {
            Console.WriteLine("There was an error downloading the video");
            return;
        }
        
        var videoPath = $"{tempDir}{videoId}.{videoInfo.Data?.Extension}";
        await ConvertVideoTask(videoPath, videoId!);
    }

    async Task ConvertVideoTask(string? videoPath, string videoId)
    {
        if (!new[] { "144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p" }.Contains(resolutionValue))
            resolutionValue = null;
        
        var resolutionChange = resolutionValue is not null;
        
        var uuid = Guid.NewGuid().ToString("N")[..8];
        
        // 1. If the user provided a file, we get the filename without the extension
        // 2. If the user provided a link, we get the video id
        // 3. If the user wants a random filename, we generate a random string
        // 4. If the filename ends with .mp4, we add _comp.mp4 to the end of the filename
        // 5. If the filename doesn't end with .mp4, we add .mp4 to the end of the filename
        // 6. We set the output path to the output folder + the filename
        var outputFilename = videoPath is not null ? Path.GetFileNameWithoutExtension(videoPath) : videoId;
        if (randomFilenameValue) outputFilename = uuid;
        if (outputFilename.EndsWith(".mp4")) outputFilename += "_comp.mp4";
        else outputFilename += ".mp4";
        var videoPathConverted = $"{outputValue}{separator}{outputFilename}";

        // If a user presses Ctrl+C, the program will delete the converted video and the log file
        Console.CancelKeyPress += (_, _) =>
        {
            if (File.Exists(videoPathConverted)) File.Delete(videoPathConverted);
            Console.WriteLine("\nCancelled");
        };

        // get video resolution
        IMediaInfo? video;
        try
        {
            video = await FFmpeg.GetMediaInfo(videoPath);
        }
        catch (ConversionException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid video file");
            Console.ResetColor();
            return;
        }
        
        // Get Video Stream Width and Height
        var videoStream = video.VideoStreams.FirstOrDefault();
        var audioStream = video.AudioStreams.FirstOrDefault();
        var videoWidth = videoStream!.Width;
        var videoHeight = videoStream.Height;
        
        // check if it's a vertical video
        var verticalVideo = videoWidth < videoHeight;

        var resolution = resolutionValue switch
        {
            "144p" => verticalVideo ? (videoWidth: 144, videoHeight: 256) : (videoWidth: 256, videoHeight: 144),
            "240p" => verticalVideo ? (videoWidth: 240, videoHeight: 426) : (videoWidth: 426, videoHeight: 240),
            "360p" => verticalVideo ? (videoWidth: 360, videoHeight: 640) : (videoWidth: 640, videoHeight: 360),
            "480p" => verticalVideo ? (videoWidth: 480, videoHeight: 854) : (videoWidth: 854, videoHeight: 480),
            "720p" => verticalVideo ? (videoWidth: 720, videoHeight: 1280) : (videoWidth: 1280, videoHeight: 720),
            "1080p" => verticalVideo ? (videoWidth: 1080, videoHeight: 1920) : (videoWidth: 1920, videoHeight: 1080),
            "1440p" => verticalVideo ? (videoWidth: 1440, videoHeight: 2560) : (videoWidth: 2560, videoHeight: 1440),
            "2160p" => verticalVideo ? (videoWidth: 2160, videoHeight: 3840) : (videoWidth: 3840, videoHeight: 2160),
            _ => (videoWidth, videoHeight)
        };

        // if a file with same name exists offer to delete it
        if (File.Exists(videoPathConverted))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("A file with the same name already exists");
            Console.ResetColor();
            Console.Write("Do you want to delete it? (y/n): ");
            var deleteFile = Console.ReadLine();
            if (deleteFile is "y" or "Y")
                File.Delete(videoPathConverted);
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not convert the video");
                Console.ResetColor();
                return;
            }
        }

        var conversion = FFmpeg.Conversions.New()
            .AddStream(videoStream)
            .AddStream(audioStream)
            .SetOutput(videoPathConverted);

        var parameter = $"-c:v libx264 -crf {crfValue}";
        parameter += resolutionChange
            ? $" -vf scale=h={resolution.videoHeight}:w={resolution.videoWidth} -pix_fmt yuv420p -c:a aac -b:a {audioBitrateValue}k"
            : $" -pix_fmt yuv420p -c:a aac -b:a {audioBitrateValue}k";

        conversion.AddParameter(parameter);
        
        conversion.OnProgress += (_, args) =>
        {
            var percent = args.Duration.TotalSeconds / args.TotalLength.TotalSeconds;
            var eta = args.TotalLength - args.Duration;
            Console.Write($"\rProgress: {percent:P2} | ETA: {eta}");
        };
        
        try
        {
            await conversion.Start();
        }
        catch (ConversionException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.ResetColor();
            return;
        }
        Console.WriteLine($"\nDone!\nConverted video saved at {outputFilename}");

        if(deleteOriginalValue) File.Delete(videoPath ?? string.Empty);
    }
}