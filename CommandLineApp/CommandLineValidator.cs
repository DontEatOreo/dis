using System.CommandLine.Parsing;
using dis.CommandLineApp.Interfaces;
using Serilog;

namespace dis.CommandLineApp;

public sealed class CommandLineValidator : ICommandLineValidator
{
    private readonly Globals _globals;
    private readonly ILogger _logger;

    public CommandLineValidator(ILogger logger, Globals globals)
    {
        _logger = logger;
        _globals = globals;
    }

    public void ValidateInputs(OptionResult result)
    {
        var inputs = result.GetValueOrDefault<string[]>();
        if (inputs is null)
        {
            _logger.Error("No input files or links were provided");
            return;
        }
        foreach (var item in inputs)
        {
            if (File.Exists(item) || Uri.IsWellFormedUriString(item, UriKind.RelativeOrAbsolute))
                continue;

            _logger.Error("Invalid input file or link: {Input}", item);
            Environment.Exit(1);
        }
    }

    public void ValidateOutput(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (Directory.Exists(input))
            return;

        const string errorMsg = "Output directory does not exist";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }

    public void ValidateCrf(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();
        if (input is >= 0 and <= 63)
            return;

        const string errorMsg = "CRF value must be between 0 and 63 (Avoid values below 20)";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }

    public void ValidateAudioBitrate(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();
        if (input % 2 is 0 && input > 0)
            return;

        const string errorMsg = "Audio bitrate must be a multiple of 2";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }

    public void ValidateVideoCodec(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        var hasKeys = _globals.ValidVideoCodecsMap.Any(kv => kv.Key.Contains(input));
        if (input is not null)
            if (hasKeys)
                return;

        const string errorMsg = "Invalid video codec";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }

    public void ValidateResolution(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (_globals.ResolutionList.Contains(input))
            return;

        const string errorMsg = "Invalid resolution";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }
}
