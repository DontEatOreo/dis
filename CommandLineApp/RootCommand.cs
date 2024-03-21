using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using dis.CommandLineApp.Conversion;
using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Microsoft.AspNetCore.StaticFiles;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dis.CommandLineApp;

public sealed partial class RootCommand(
    FileExtensionContentTypeProvider type,
    ILogger logger,
    Globals globals,
    IDownloader downloader,
    Converter converter)
    : AsyncCommand<Settings>
{
    private void ValidateInputs(IEnumerable<string> inputs)
    {
        foreach (var input in inputs)
        {
            var isPath = File.Exists(input);
            var isUrl = Uri.IsWellFormedUriString(input, UriKind.RelativeOrAbsolute);
            switch (isPath)
            {
                case false when isUrl is false:
                    {
                        ValidationResult.Error($"Invalid input file or link: {input}");
                        return;
                    }
                case true:
                    {
                        if (type.TryGetContentType(input, out var contentType) is false) return;
                        if (contentType.Contains("video") || contentType.Contains("audio")) return;
                        break;
                    }
            }
        }
    }

    private static void ValidateOutput(string? output)
    {
        if (string.IsNullOrEmpty(output)) output = Environment.CurrentDirectory;
        if (Directory.Exists(output) is false)
            ValidationResult.Error("Output directory does not exist");
    }

    private static void ValidateCrf(int crf)
    {
        const int min = 6;
        const int minRecommended = 22;
        const int max = 63;
        const int maxRecommended = 38;

        var settingsType = typeof(Settings);
        // Use reflection to get the Crf property in the Settings class.
        var crfProperty = settingsType.GetProperty(nameof(Settings.Crf));

        // Get the DefaultValueAttribute assigned to the Crf property.
        var defaultValueAttribute = crfProperty?
            .GetCustomAttributes(typeof(DefaultValueAttribute), false)
            .FirstOrDefault() as DefaultValueAttribute;

        // Get the value from the DefaultValueAttribute.
        var defaultValue = (int)defaultValueAttribute?.Value!;

        /*
         * Use pattern matching with a switch expression to check if the 'crf' value is valid.
         * If 'crf' is less than 0 or greater than the maximum, it is invalid, so 'validCrf' will be set to false.
         * If 'crf' is between the minimum and maximum, inclusive, 'crf' is valid, and 'validCrf' will be set to true.
         */
        var validCrf = crf switch
        {
            < 0 => false,
            >= min and <= max => true,
            _ => false
        };

        if (validCrf is false)
            ValidationResult.Error($"CRF value must be between {min} and {max} (Avoid values below {defaultValue})");
        else switch (crf)
        {
            case < minRecommended:
                AnsiConsole.MarkupLine($"[yellow]CRF values below {minRecommended} are not recommended[/]");
                break;
            case > maxRecommended:
                AnsiConsole.MarkupLine($"[yellow]CRF values above {maxRecommended} are not recommended[/]");
                break;
        }
    }

    private static void ValidateAudioBitrate(int? audioBitrate)
    {
        if (audioBitrate is null) return;

        var audioBitrateRange = audioBitrate switch
        {
            < 128 => false,
            > 192 => false,
            _ => true
        };
        if (audioBitrateRange is false)
            AnsiConsole.MarkupLine("[yellow]Audio bitrate values below 128 or above 192 are not recommended[/]");
        if (audioBitrate % 2 != 0)
            ValidationResult.Error("Audio bitrate must be a multiple of 2");
    }

    private static void ValidateTrim(string? trim)
    {
        if (string.IsNullOrEmpty(trim)) return;

        var input = trim.Replace("*", "");
        var match = TrimRegex().Match(input);

        if (match.Success is false)
        {
            ValidationResult.Error("Trim values must be in the format ss.ms-ss.ms or ss-ss");
            return;
        }

        var span = input.AsSpan();
        var dashIndex = span.IndexOf('-');
        var startTimeStr = span[..dashIndex].ToString();
        var endTimeStr = span[(dashIndex + 1)..].ToString();

        if (decimal.TryParse(startTimeStr, out var startTime)
            && decimal.TryParse(endTimeStr, out var endTime))
        {
            if (startTime > endTime)
                ValidationResult.Error("Start time should not be later than end time in the 'trim' value");

            if (startTime == endTime)
                ValidationResult.Error("'trim' value should not have same start and end times");
        }
        else
            ValidationResult.Error("'trim' value contains un-parsable numbers");
    }

    private void ValidateResolution(string? resolution)
    {
        var hasResolution = resolution is not null;
        if (hasResolution is false) return;

        var validResolution = globals.ValidResolutions
            .Any(res => res.ToString().Equals($"{resolution}p", StringComparison.InvariantCultureIgnoreCase));
        if (validResolution is false)
            ValidationResult.Error("Invalid resolution");
    }

    private void ValidateVideoCodec(string? videoCodec)
    {
        var hasVideoCodec = videoCodec is not null;
        if (!hasVideoCodec) return;

        var validVideoCodec = globals.VideoCodecs.Values
            .Any(codec => codec.ToString()
                .Equals(videoCodec,
                StringComparison.InvariantCultureIgnoreCase));
        if (validVideoCodec is false)
            ValidationResult.Error("Invalid video codec");
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        ValidateInputs(settings.Input);
        ValidateOutput(settings.Output);
        ValidateCrf(settings.Crf);
        ValidateAudioBitrate(settings.AudioBitrate);
        ValidateTrim(settings.Trim);
        ValidateResolution(settings.Resolution);
        ValidateVideoCodec(settings.VideoCodec);
        ValidationResult.Success();
        return base.Validate(context, settings);
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // Patch up output directory to current dir, if it's empty
        if (string.IsNullOrEmpty(settings.Output)) settings.Output = Environment.CurrentDirectory;

        var links = settings.Input
            .Where(video =>
                Uri.IsWellFormedUriString(video, UriKind.RelativeOrAbsolute) &&
                File.Exists(video) is false)
            .Select(video => new Uri(video));

        var files = settings.Input
            .Where(File.Exists);

        var paths = new Dictionary<string, DateTime?>();
        
        if (await CheckForFFmpegAndYtDlp() is false)
            return 1;
        
        await Download(links, paths, settings);

        foreach (var file in files)
            paths.TryAdd(file, null);

        await Convert(paths, settings);

        return 0;
    }
    
    private static async Task<bool> CheckForFFmpegAndYtDlp()
    {
        try
        {
            var cmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var ffmpegPath = await GetCommandPath(cmd, "ffmpeg");
            var ytDlpPath = await GetCommandPath(cmd, "yt-dlp");

            if (string.IsNullOrWhiteSpace(ffmpegPath))
            {
                AnsiConsole.WriteLine("FFmpeg not found in PATH. Please install FFmpeg and add it to PATH.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ytDlpPath))
            {
                AnsiConsole.WriteLine("yt-dlp not found in PATH. Please install yt-dlp and add it to PATH.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }
    }

    private static async Task<string> GetCommandPath(string cmd, string commandName)
    {
        ProcessStartInfo processInfo = new(cmd, commandName)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        var commandPath = await process?.StandardOutput.ReadToEndAsync()!;
        await process.WaitForExitAsync();

        return commandPath;
    }

    private async Task Download(IEnumerable<Uri> links, Dictionary<string, DateTime?> videos, Settings options)
    {
        var list = links.ToList();
        if (list.Count is 0)
            return;

        foreach (var downloadOptions in list.Select(link => new DownloadOptions(link, options)))
        {
            var (path, date) = await downloader.DownloadTask(downloadOptions);

            if (path is null)
                logger.Error("There was an error downloading the video");
            else
            {
                var added = videos.TryAdd(path, date);
                if (added is false)
                    logger.Error("Failed to add video to list: {Path}", path);
            }
        }

        foreach (var path in videos.Keys) AnsiConsole.MarkupLine($"Downloaded video to: [green]{path}[/]");
    }

    private async Task Convert(IEnumerable<KeyValuePair<string, DateTime?>> videos, Settings options)
    {
        foreach (var (path, date) in videos)
        {
            try
            {
                await converter.ConvertVideo(path, date, options);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to convert video: {Path}", path);
            }
        }

        var hasAny = globals.TempDir.Count is not 0;
        if (hasAny is false)
            return;

        globals.TempDir.ForEach(d =>
        {
            Directory.Delete(d, true);
            AnsiConsole.MarkupLine($"Deleted temp dir: [red]{d}[/]");
        });
    }

    /// <summary>
    /// This regex pattern is used to parse time range strings in 'ss.ms-ss.ms' or 'ss-ss' format.
    /// Here, 'ss' are one or more digits representing seconds, while 'ms' are one or two optional digits for milliseconds.
    /// </summary>
    ///
    /// <remarks>
    /// The pattern breakdown is as follows:
    /// '^' : Starts the pattern.
    /// '\d+' : Matches one or more digits (seconds part).
    /// '(\.\d{1,2})?' : Optionally matches a dot and one or two digits (milliseconds part).
    /// '-' : Matches the dash separating the start and end times.
    /// '\d+' : Matches one or more digits for the end time seconds part.
    /// '(\.\d{1,2})?' : Again, optionally matches a dot and one or two digits for end time milliseconds.
    /// '$' : Ends the pattern.
    ///
    /// If milliseconds part is present, it must have one or two digits only.
    /// </remarks>
    ///
    /// <example>
    /// "222.22-222.22" is valid, having digits, optional dot and one or two digits after dot, separated by a dash.
    /// "222.222-222.222" is invalid, having three digits after the dot is not allowed.
    /// </example>
    [GeneratedRegex(@"^\d+(\.\d{1,2})?-\d+(\.\d{1,2})?$", RegexOptions.Compiled)]
    private static partial Regex TrimRegex();
}
