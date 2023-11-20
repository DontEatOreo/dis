using System.CommandLine.Parsing;
using dis.CommandLineApp.Interfaces;
using Microsoft.AspNetCore.StaticFiles;
using Serilog;

namespace dis.CommandLineApp;

public sealed class CommandLineValidator(ILogger logger, IContentTypeProvider type, Globals globals) : ICommandLineValidator
{
    private readonly string[] _resolutionList =
    [
        "144p",
        "240p",
        "360p",
        "480p",
        "720p",
        "1080p",
        "1440p",
        "2160p"
    ];

    public void Inputs(OptionResult result)
    {
        var inputs = result.GetValueOrDefault<string[]>();
        foreach (var item in inputs)
        {
            if (File.Exists(item))
            {
                if (!type.TryGetContentType(item, out var contentType)) continue;
                if (contentType.Contains("video") || contentType.Contains("audio"))
                    return;
            }
            else if (Uri.IsWellFormedUriString(item, UriKind.RelativeOrAbsolute))
                return;
            else
                result.AddError($"Invalid input file or link: {item}");
        }
    }

    public void Output(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (Directory.Exists(input))
            return;

        result.AddError("Output directory does not exist");
    }

    public void MultiThread(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();
        var threads = Environment.ProcessorCount * 2;

        if (input > 16)
        {
            logger.Information("Due to the way FFmpeg works, anything more than 16 threads will be ignored");
            input = 16;
        }

        if (input <= threads)
            return;

        result.AddError($"Number of threads cannot be greater than {threads}");
    }

    public void Crf(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();

        const int min = 6;
        const int max = 63;

        var validCrf = input switch
        {
            < 0 => false,
            >= min and <= max => true,
            _ => false
        };
        if (validCrf)
            return;

        result.AddError($"CRF value must be between {min} and {max} (Avoid values below 22)");
    }

    public void AudioBitRate(OptionResult result)
    {
        var input = result.GetValueOrDefault<long>();

        if (input < 0)
            result.AddError("Audio bitrate cannot be negative");

        var validBitrate = input % 2 is 0 && input > 0;
        if (validBitrate)
            return;

        result.AddError("Audio bitrate must be a multiple of 2");
    }

    public void VideoCodec(OptionResult result)
    {
        var input = result.GetValueOrDefault<string?>();
        var hasKeys = globals.VideoCodecs.Any(kv => kv.Key.Contains(input));
        if (input is not null)
            if (hasKeys)
                return;

        result.AddError("Invalid video codec");
    }

    public void Resolution(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (_resolutionList.Contains(input))
            return;

        result.AddError("Invalid resolution");
    }

    public void Trim(OptionResult result)
    {
        var input = result.GetValueOrDefault<string?>();
        if (input is null)
            return;

        var span = input.AsSpan();
        // Find the position of the dash in the span.
        var dashIndex = span.IndexOf('-');

        // If there's no dash, show an error and stop.
        if (dashIndex is -1)
            result.AddError("Trim values must be in the format ss.ms-mm.ms");

        // Split the span into two parts at the dash.
        var startSpan = span[..dashIndex];
        var endSpan = span[(dashIndex + 1)..];

        // Try to convert both parts to floats. If either fails, show an error and stop.
        var start = float.Parse(startSpan);
        var end = float.Parse(endSpan);

        // If either value is negative, show an error and stop.
        if (start < 0 || end < 0)
            result.AddError("Trim values cannot be negative");

        // If the start value is greater than the end value, show an error and stop.
        if (start > end)
            result.AddError("Start value cannot be greater than end value");

        // If the difference between the start and end values is less than 1, show an error and stop.
        if (end - start < 1)
            result.AddError("Trim values must be at least 1 second apart");
    }
}
