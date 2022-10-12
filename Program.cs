using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

RootCommand rootCommand = new();

YoutubeDl youtubeDl = new()
{
    OutputFileTemplate = "%(id)s.%(ext)s",
    OverwriteFiles = false,
};

Regex twitterRegex = new(@"^1[45]\d{17}$", RegexOptions.Compiled);
Regex youtubeRegex = new(@"^([a-zA-Z0-9_-]{11})$", RegexOptions.Compiled);
Regex redditRegex = new(@"^[xy][a-zA-Z0-9]{5}$", RegexOptions.Compiled);
Regex tiktokRegex = new(@"^[67]\d{18}$", RegexOptions.Compiled);

Dictionary<Regex, string> urlRegex = new()
{
    { twitterRegex, "https:/twitter.com/i/status/" },
    { youtubeRegex, "https://youtu.be/" },
    { redditRegex, "https://reddit.com/" },
    { tiktokRegex, "https://www.tiktok.com/@dis/video/" }
};

var sep = Path.DirectorySeparatorChar;

Option<string> fileInput = new(new[] { "-i", "--input" }, "A path to a video file");
fileInput.AddValidator(validate =>
{
    if (File.Exists(validate.Token?.Value)) return;
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
    var resolutions = new[] { "144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p" };
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
videoCodecInput.AddCompletions("libx264", "libx265", "libvpx", "libvpx-vp9", "libaom-av1");
videoCodecInput.AddValidator(validate =>
{
    var videoCodecValue = validate.GetValueOrDefault<string>();
    var videoCodecs = new[] { "libx264", "libx265", "libvpx", "libvpx-vp9", "libaom-av1" };
    if (videoCodecs.Contains(videoCodecValue)) return;
    Console.WriteLine("Invalid video codec");
    Environment.Exit(1);
});

rootCommand.TreatUnmatchedTokensAsErrors = true;

foreach(var option in new Option[] { fileInput, linkInput, output , crfInput, resolutionInput, audioBitrateInput, audioCodecInput, videoCodecInput })
    rootCommand.AddOption(option);

if (args.Length == 0) args = new[] { "-h" };

rootCommand.SetHandler(HandleInput);

await rootCommand.InvokeAsync(args);

async Task HandleInput(InvocationContext invocationContext)
{
    var fileValue = invocationContext.ParseResult.GetValueForOption(fileInput);
    var linkValue = invocationContext.ParseResult.GetValueForOption(linkInput);
    var outputValue = invocationContext.ParseResult.GetValueForOption(output);
    var crfValue = invocationContext.ParseResult.GetValueForOption(crfInput);
    var resolutionValue = invocationContext.ParseResult.GetValueForOption(resolutionInput);
    var audioBitrateValue = invocationContext.ParseResult.GetValueForOption(audioBitrateInput);
    var audioCodecValue = invocationContext.ParseResult.GetValueForOption(audioCodecInput);
    var videoCodecValue = invocationContext.ParseResult.GetValueForOption(videoCodecInput);

    youtubeDl.OutputFolder = outputValue!;
    string? parameter;

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

        RunResult<VideoData>? videoInfo;
        try
        {
            videoInfo = await youtubeDl.RunVideoDataFetch(linkValue);
        }
        catch (Exception)
        {
            Console.WriteLine("Invalid link");
            return;
        }
        var videoId = videoInfo.Data.Id;
        
        var progress = new Progress<DownloadProgress>(p => 
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (p.Progress is 0) return;
            Console.Write($"Progress: {p.Progress:P2} Download speed: {p.DownloadSpeed}  \r");
        });
        
        RunResult<string>? videoDownload;
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
        
        var videoPath = $"{outputValue}{sep}{videoId}.{videoInfo.Data.Extension}";
        
        await ConvertVideoTask(videoPath, videoId);
    }

    async Task ConvertVideoTask(string videoPath, string videoId)
    {
        var videoExtension = videoCodecValue switch
        {
            "libx264" => "mp4",
            "libx265" => "mp4",
            "libvpx" => "mkv",
            "libvpx-vp9" => "webm",
            "libaom-av1" => "mkv",
            _ => "mp4"
        };
       
        // If compressed file already exists, we delete it
        var compPath = $"{outputValue}{sep}{videoId}-comp.{videoExtension}";
        if (File.Exists(compPath))
            File.Delete(compPath);

        if (!new[] { "144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p" }.Contains(resolutionValue))
            resolutionValue = null;

        if (resolutionValue != null)
        {
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
            var isVertical = videoWidth < videoHeight;

            // get the resolution
            var resolution = resolutionValue.Replace("p", "");

            // if video is vertical, swap width and height
            // 16:9 = 16 / 9
            // 9:16 = 9 / 16
            var (width, height) = isVertical
                ? (int.Parse(resolution), int.Parse(resolution) * 9 / 16)
                : (int.Parse(resolution) * 16 / 9, int.Parse(resolution));

            parameter =
                $"-i {videoPath} -c:v {videoCodecValue} " +
                $"-crf {crfValue} -c:a {audioCodecValue} " +
                $"-b:a {audioBitrateValue}k " +
                $"-vf scale={width}:{height} {outputValue}{sep}{videoId}-comp.{videoExtension}";
        }
        else
        {
            parameter =
                $"-i {videoPath} -c:v {videoCodecValue} " +
                $"-crf {crfValue} -c:a {audioCodecValue} " +
                $"-b:a {audioBitrateValue}k {outputValue}{sep}{videoId}-comp.{videoExtension}";
        }


        var conversion = FFmpeg.Conversions
            .New()
            .AddParameter(parameter);

        conversion.OnProgress += (_, args) =>
        {
            Console.ForegroundColor = (ConsoleColor)new Random().Next(1, 16);
            Console.Write(
                $"\r[{args.Duration} / {args.TotalLength}] {args.Duration.TotalSeconds / args.TotalLength.TotalSeconds:P2}");
        };

        await conversion.Start();
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Done");
    }
}