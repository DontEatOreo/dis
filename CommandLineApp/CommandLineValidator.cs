using System.CommandLine.Parsing;
using dis.CommandLineApp.Interfaces;
using Serilog;

namespace dis.CommandLineApp;

public sealed class CommandLineValidator : ICommandLineValidator
{
    private readonly Globals _globals;
    private readonly ILogger _logger;

    private readonly string[] _resolutionList =
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

    public CommandLineValidator(ILogger logger, Globals globals)
    {
        _logger = logger;
        _globals = globals;
    }

    public void Inputs(OptionResult result)
    {
        var inputs = result.GetValueOrDefault<string[]>();
        foreach (var item in inputs)
        {
            if (File.Exists(item) || Uri.IsWellFormedUriString(item, UriKind.RelativeOrAbsolute))
                continue;

            _logger.Error("Invalid input file or link: {Input}", item);
            Environment.Exit(1);
        }
    }

    public void Output(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (Directory.Exists(input))
            return;

        const string errorMsg = "Output directory does not exist";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }

    public void MultiThread(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();
        var threads = Environment.ProcessorCount * 2;

        if (input > 16)
        {
            _logger.Information("Due to the way FFmpeg works, anything more than 16 threads will be ignored");
            input = 16;
        }

        if (input <= threads)
            return;

        _logger.Error("Number of threads cannot be greater than {Threads}", threads);
        Environment.Exit(1);
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

        _logger.Error("CRF value must be between {Min} and {Max} (Avoid values below 22)",
            min, max);
        Environment.Exit(1);
    }

    public void AudioBitRate(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();

        if (input < 0)
        {
            _logger.Error("Audio bitrate cannot be negative");
            Environment.Exit(1);
        }

        var validBitrate = input % 2 is 0 && input > 0;
        if (validBitrate)
            return;

        _logger.Error("Audio bitrate must be a multiple of 2");
        Environment.Exit(1);
    }

    public void VideoCodec(OptionResult result)
    {
        var input = result.GetValueOrDefault<string?>();
        var hasKeys = _globals.VideoCodecs.Any(kv => kv.Key.Contains(input));
        if (input is not null)
            if (hasKeys)
                return;

        _logger.Error("Invalid video codec");
        Environment.Exit(1);
    }

    public void Resolution(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (_resolutionList.Contains(input))
            return;

        _logger.Error("Invalid resolution");
        Environment.Exit(1);
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
        {
            _logger.Error("Invalid trim format");
            Environment.Exit(1);
        }

        // Split the span into two parts at the dash.
        var startSpan = span[..dashIndex];
        var endSpan = span[(dashIndex + 1)..];

        // Try to convert both parts to floats. If either fails, show an error and stop.
        var start = float.Parse(startSpan);
        var end = float.Parse(endSpan);

        // If either value is negative, show an error and stop.
        if (start < 0 || end < 0)
        {
            _logger.Error("Trim values must be positive");
            Environment.Exit(1);
        }

        // If the start value is greater than the end value, show an error and stop.
        if (start > end)
        {
            _logger.Error("Start value must be lower than end value");
            Environment.Exit(1);
        }

        // If the difference between the start and end values is less than 1, show an error and stop.
        if (end - start < 1)
        {
            _logger.Error("Trim values must be at least 1 second apart");
            Environment.Exit(1);
        }
    }
}
