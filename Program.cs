using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using dis.YoutubeDLSharp;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;
using YoutubeDLSharp.Metadata;

RootCommand rootCommand = new();

YoutubeDl youtubeDl = new()
{
    OutputFileTemplate = "%(id)s.%(ext)s",
    OverwriteFiles = false,
};

Regex twitterRegex = new(@"^1[45]\d{17}$", RegexOptions.Compiled);
Regex youtubeRegex = new(@"^([a-zA-Z0-9_-]{11})$", RegexOptions.Compiled);
Regex redditRegex = new(@"^x[a-zA-Z0-9]{5}$", RegexOptions.Compiled);
Regex tiktokRegex = new(@"^[67]\d{18}$", RegexOptions.Compiled);

Dictionary<Regex, string> urlRegex = new()
{
    { twitterRegex, "https:/twitter.com/i/status/" },
    { youtubeRegex, "https://youtu.be/" },
    { redditRegex, "https://reddit.com/" },
    { tiktokRegex, "https://www.tiktok.com/@dis/video/" }
};

var sep = Path.DirectorySeparatorChar;

Option<bool> verbose = new(new[] { "-v", "--verbose" }, "Enable verbose logging");

Option<string> fileInput = new(new[] { "-i", "--input" }, "A path to a video file");
fileInput.AddValidator(validate =>
{
    var file = validate.GetValueOrDefault<string>();
    if (File.Exists(file)) return;
    Console.WriteLine("File does not exist");
    Environment.Exit(1);
});

Option<string> linkInput = new(new[] { "-l", "--link" }, "A URL link to a video");
linkInput.AddValidator(validate =>
{
    if (urlRegex.Any(x => x.Key.IsMatch(validate.Token?.Value!)) ||
        Uri.IsWellFormedUriString(validate.Tokens[0].Value, UriKind.RelativeOrAbsolute)) return;
    Console.WriteLine("Invalid URL");
    Environment.Exit(1);
});

Option<string> output = new(new[] { "-o", "--output" }, "Directory to save the compressed video to");
output.SetDefaultValue(Environment.CurrentDirectory);
output.AddValidator(validate =>
{
    var outputValue = validate.GetValueOrDefault<string>();
    if (Directory.Exists(outputValue)) return;
    Console.WriteLine("Output directory does not exist");
    Environment.Exit(1);
});

Option<int> crfInput = new(new[] { "-c", "--crf" }, "CRF value");
crfInput.SetDefaultValue(26);
crfInput.AddValidator(validate =>
{
    var crfValue = validate.GetValueOrDefault<int>();
    if (crfValue is >= 0 and <= 51) return;
    Console.WriteLine("CRF value must be between 0 and 51");
    Environment.Exit(1);
});

Option<string> resolutionInput = new(new[] { "-r", "--resolution" }, "Resolution");
resolutionInput.AddCompletions("144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p");
resolutionInput.AddValidator(validate =>
{
    var resolutionValue = validate.GetValueOrDefault<string>();
    var resolutions = new[] { "144", "240", "360", "480", "720", "1080", "1440", "2160" };
    if (resolutions.Contains(resolutionValue)) return;
    Console.WriteLine("Invalid resolution");
    Environment.Exit(1);
});

Option<string> audioBitrateInput = new(new[] { "-a", "--audio-bitrate" }, "Audio bitrate");
audioBitrateInput.SetDefaultValue("128");
audioBitrateInput.AddCompletions("32", "64", "96", "128", "192", "256", "320");
audioBitrateInput.AddValidator(validate =>
{
    var audioBitrateValue = validate.GetValueOrDefault<string>();
    var audioBitrates = new[] { "32", "64", "96", "128", "192", "256", "320" };
    if (audioBitrates.Contains(audioBitrateValue)) return;
    Console.WriteLine("Invalid audio bitrate");
    Environment.Exit(1);
});

Option<string> audioCodecInput = new(new[] { "-ac", "--audio-codec" }, "Audio codec");
audioCodecInput.SetDefaultValue("aac");
audioCodecInput.AddCompletions("aac", "libmp3lame", "libopus", "libvorbis");
audioCodecInput.AddValidator(validate =>
{
    var audioCodecValue = validate.GetValueOrDefault<string>();
    var audioCodecs = new[] { "aac", "libmp3lame", "libopus", "libvorbis" };
    if (audioCodecs.Contains(audioCodecValue)) return;
    Console.WriteLine("Invalid audio codec");
    Environment.Exit(1);
});

Option<string> videoCodecInput = new(new[] { "-vc", "--video-codec" }, "Video codec");
videoCodecInput.SetDefaultValue("libx264");
videoCodecInput.AddCompletions("libx264", "libx265", "libvpx-vp9");
videoCodecInput.AddValidator(validate =>
{
    var videoCodecValue = validate.GetValueOrDefault<string>();
    var videoCodecs = new[] { "libx264", "libx265", "libvpx-vp9" };
    if (videoCodecs.Contains(videoCodecValue)) return;
    Console.WriteLine("Invalid video codec");
    Environment.Exit(1);
});

rootCommand.TreatUnmatchedTokensAsErrors = true;

foreach(var option in new Option[] { verbose, fileInput, linkInput, output , crfInput, resolutionInput, audioBitrateInput, audioCodecInput, videoCodecInput })
    rootCommand.AddOption(option);

if (args.Length == 0) args = new[] { "-h" };

rootCommand.SetHandler(HandleInput);

await rootCommand.InvokeAsync(args);

async Task HandleInput(InvocationContext invocationContext)
{
    var verboseValue = invocationContext.ParseResult.GetValueForOption(verbose);
    var fileValue = invocationContext.ParseResult.GetValueForOption(fileInput);
    var linkValue = invocationContext.ParseResult.GetValueForOption(linkInput);
    var outputValue = invocationContext.ParseResult.GetValueForOption(output);
    var crfValue = invocationContext.ParseResult.GetValueForOption(crfInput);
    var resolutionValue = invocationContext.ParseResult.GetValueForOption(resolutionInput);
    var audioBitrateValue = invocationContext.ParseResult.GetValueForOption(audioBitrateInput);
    var audioCodecValue = invocationContext.ParseResult.GetValueForOption(audioCodecInput);
    var videoCodecValue = invocationContext.ParseResult.GetValueForOption(videoCodecInput);

    youtubeDl.OutputFolder = outputValue!;
    
    if (videoCodecValue == "libvpx-vp9") audioCodecValue = "libopus";

    if (fileValue == null && linkValue == null)
    {
        Console.WriteLine("You must provide either a file or a link");
        return;
    }

    if (fileValue != null && linkValue == null)
    {
        var fileName = Path.GetFileNameWithoutExtension(fileValue);
        await ConvertVideoTask(fileValue, fileName);
    }

    if (fileValue == null && linkValue != null)
    {
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
        var videoId = videoInfo.Data?.Id;
        
        var progress = new Progress<DownloadProgress>(p => 
        {
            if (p.Progress is 0) return;
            Console.Write($"Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}\t\r");
        });
        
        RunResult<string?> videoDownload;
        try
        {
            videoDownload = await youtubeDl.RunVideoDownload(linkValue, progress: progress);
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
        
        var videoPath = $"{outputValue}{sep}{videoId}.{videoInfo.Data?.Extension}";
        
        await ConvertVideoTask(videoPath, videoId!);
    }

    async Task ConvertVideoTask(string videoPath, string videoId)
    {
        var videoExtension = videoCodecValue switch
        {
            "libx264" => "mp4",
            "libx265" => "mp4",
            "libvpx-vp9" => "webm",
            _ => throw new Exception("Invalid video codec")
        };

        if (!new[] { "144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p" }.Contains(resolutionValue))
            resolutionValue = null;
        
        var ffmpegSeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "NUL;" : "/dev/null ";
        
        var resolutionChange = resolutionValue != null;

        string? libxParam;
        string? libvpxVp9ParamPostInput;
        var videoPathConverted = 
            $"{outputValue}{sep}{videoId}{(videoExtension != Path.GetExtension(videoPath).Replace(".", "") ? $".{videoExtension}" : $"-comp.{videoExtension}")}";

        // get video resolution
        IMediaInfo? video;
        try
        {
            video = await FFmpeg.GetMediaInfo(videoPath);
        }
        catch (ArgumentException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid video file");
            Console.ResetColor();
            return;
        }
        
        // Get Video Stream Width and Height
        var videoStream = video.VideoStreams.First();
        var videoWidth = videoStream.Width;
        var videoHeight = videoStream.Height;
        
        // check if it's a vertical video
        // We do this by checking if the video width is less than the video height
        var isVertical = videoWidth < videoHeight;

        string? resolution;
        // (int, int) width = (0,0), height = (0, 0);
        int width = 0, height = 0;
        try
        {
            // get the resolution
            resolution = resolutionValue?.Replace("p", "");

            // The width of a video is always 4/3 of the height
            var resolutionHeight = int.Parse(resolution);
            var resolutionWidth = (int) (resolutionHeight * 4 / 3.0);
        
            // The width and height are swapped if the video is vertical
            width = isVertical ? resolutionHeight : resolutionWidth;
            height = isVertical ? resolutionWidth : resolutionHeight;
        }
        catch (Exception)
        {
            // ignored
        }

        libxParam = $"-i {videoPath} " +
                    (resolutionChange ? $"-vf scale={width}:{height} " : "") +
                    $"-c:v {videoCodecValue} " +
                    $"-crf {crfValue} " +
                    $"-c:a {audioCodecValue} " +
                    $"-b:a {audioBitrateValue}k " +
                    $"{videoPathConverted}";

        var libvpxVp9ParamPreInput = $"-i {videoPath} " +
                                     $"{(resolutionChange ? $"-vf scale={width}:{height} " : "")}" +
                                     $"-c:v {videoCodecValue} " +
                                     "-pix_fmt yuv420p10le " +
                                     "-pass 1 " +
                                     "-quality good " +
                                     "-threads 4 " +
                                     "-profile:v 2 " +
                                     "-lag-in-frames 25 " +
                                     $"-crf {crfValue} " +
                                     "-b:v 0 " +
                                     "-g 240 " +
                                     "-cpu-used 0 " +
                                     "-auto-alt-ref 1 " +
                                     "-arnr-maxframes 7 " +
                                     "-arnr-strength 4 " +
                                     "-aq-mode 0 " +
                                     "-tile-rows 0 " +
                                     "-tile-columns 1 " +
                                     "-enable-tpl 1 " +
                                     "-row-mt 1 " +
                                     $"-an -f null {ffmpegSeparator} ";

        libvpxVp9ParamPostInput = $"-i {videoPath} " +
                                  $"{(resolutionChange ? $"-vf scale={width}:{height} " : "")}" +
                                  $"-c:v {videoCodecValue} " +
                                  $"-c:a {audioCodecValue} " +
                                  "-pix_fmt yuv420p10le " +
                                  "-pass 2 " + 
                                  "-quality good " +
                                  "-threads 4 " +
                                  "-profile:v 2 " +
                                  "-lag-in-frames 25 " +
                                  $"-crf {crfValue} " +
                                  "-b:v 0 " +
                                  "-g 240 " +
                                  "-cpu-used 0 " +
                                  "-auto-alt-ref 1 " +
                                  "-arnr-maxframes 7 " +
                                  "-arnr-strength 4 " +
                                  "-aq-mode 0 " +
                                  "-tile-rows 0 " +
                                  "-tile-columns 1 " +
                                  "-enable-tpl 1 " +
                                  "-row-mt 1 " +
                                  $"-b:a {audioBitrateValue}k {videoPathConverted}";

        if (File.Exists(videoPathConverted))
        {
            Console.WriteLine("A file with same name already exists. Do you want to overwrite it? (y/n)");
            var answer = Console.ReadLine();
            if (answer is not "y" or "Y")
            {
                Console.WriteLine("Aborting...");
                return;
            }
            File.Delete(videoPathConverted);
        }
        
        switch (videoCodecValue)
        {
            case "libx264" or "libx265":
            {
                var conversion = FFmpeg.Conversions.New()
                    .AddParameter(libxParam);
                
                conversion.OnProgress += (_, args) =>
                {
                    Console.ForegroundColor = (ConsoleColor) new Random().Next(1, 16);
                    var percent = args.Duration.TotalSeconds / args.TotalLength.TotalSeconds;
                    var eta = args.TotalLength - args.Duration;
                    Console.Write($"\rProgress: {percent:P2} | ETA: {eta}");
                    Console.ResetColor();
                };

                try
                {
                    await conversion.Start();
                }
                catch (ConversionException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Something went wrong");
                    if (verboseValue) Console.WriteLine(e);
                    Console.ResetColor();
                    return;
                }
                
                break;
            }
            case "libvpx-vp9":
            {
                var libvpxVp9Pass1 = FFmpeg.Conversions.New()
                    .AddParameter(libvpxVp9ParamPreInput);
                var libvpxVp9Pass2 = FFmpeg.Conversions.New()
                    .AddParameter(libvpxVp9ParamPostInput);

                libvpxVp9Pass2.OnProgress += (_, args) =>
                {
                    Console.ForegroundColor = (ConsoleColor) new Random().Next(1, 16);
                    var percent = args.Duration.TotalSeconds / args.TotalLength.TotalSeconds;
                    var eta = args.TotalLength - args.Duration;
                    Console.Write($"\rProgress: {percent:P2} | ETA: {eta}");
                    Console.ResetColor();
                };

                try
                {
                    await libvpxVp9Pass1.Start();
                }
                catch (ConversionException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Something went wrong");
                    if (verboseValue) Console.WriteLine(e);
                    Console.ResetColor();
                    return;
                }
                Console.WriteLine("Pass 1 done");
                try
                {
                    await libvpxVp9Pass2.Start();
                }
                catch (ConversionException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (verboseValue) Console.WriteLine(e);
                    if (e.Message.Contains("corrupt decoded")) Console.WriteLine("Corrupted Video!");
                    if (e.Message.Contains("Only VP8 or VP9 or AV1 video and Vorbis or Opus")) 
                        Console.WriteLine("You cannot use AAC audio codec with VP9. Please use Vorbis or Opus.");
                    Console.ResetColor();
                    return;
                }

                break;
            }
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid video codec");
                Console.ResetColor();
                return;
        }
        
        Console.WriteLine();
        Console.WriteLine("Done");
    }
}