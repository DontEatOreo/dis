using System.CommandLine;
using System.CommandLine.Parsing;
using Serilog;

namespace dis.CommandLineApp;

public sealed class CommandLineOptions
{
    private readonly Globals _globals;
    private readonly ILogger _logger;

    public CommandLineOptions(ILogger logger, Globals globals)
    {
        _logger = logger;
        _globals = globals;
    }

    /// <summary>
    /// Creates the command line options for the application.
    /// </summary>
    /// <returns>A tuple of a RootCommand and a RunOptions object.</returns>
    public Task<(RootCommand, RunOptions)> GetCommandLineOptions()
    {
        RootCommand rootCommand = new();

        #region Options
        Option<bool> randomFileName =
            new(new[] { "-rn", "-rd", "-rnd", "--random" }, "Randomize the filename");
        randomFileName.SetDefaultValue(false);

        Option<bool> keepWatermark =
            new(new[] { "-k", "-kw", "-kwm", "--keep" }, "Keep the watermark");
        keepWatermark.SetDefaultValue(false);

        Option<bool> sponsorBlock =
            new(new[] { "-sb", "-sponsorblock", "--sponsorblock" }, "Remove the sponsorblock from the video");
        keepWatermark.SetDefaultValue(false);

        Option<string[]> input =
            new(new[] { "-i", "--input", "-f", "--file" },
                    "A path to a video file or a link to a video")
                { AllowMultipleArgumentsPerToken = true, IsRequired = true };
        input.AddValidator(ValidateInputs);

        Option<string> output =
            new(new[] { "-o", "--output" },
                "Directory to save the compressed video to\n");
        output.SetDefaultValue(Environment.CurrentDirectory);
        output.AddValidator(ValidateOutput);

        Option<string> videoCodec =
            new(new[] { "-vc", "--codec", "--video-codec" }, "Video codec");
        foreach (var key in _globals.ValidVideoCodesMap.Keys)
            videoCodec.AddCompletions(key);
        videoCodec.AddValidator(ValidateVideoCodec);

        Option<int> crf =
            new(new[] { "-c", "--crf" }, "CRF value");
        crf.SetDefaultValue(29);
        crf.AddValidator(ValidateCrf);

        Option<string> resolution =
            new(new[] { "-r", "--resolution" }, "Resolution");
        resolution.AddCompletions(_globals.ResolutionList);
        resolution.AddValidator(ValidateResolution);

        Option<int> audioBitrate =
            new(new[] { "-a", "-ab", "--audio-bitrate" }, "Audio bitrate\nPossible values: 32, 64, 96, 128, 192, 256, 320");
        audioBitrate.SetDefaultValue(128);
        audioBitrate.AddValidator(ValidateAudioBitrate);

        #endregion Options

        rootCommand.TreatUnmatchedTokensAsErrors = true;

        Option[] options =
        {
            randomFileName,
            input,
            output,
            crf,
            resolution,
            audioBitrate,
            keepWatermark,
            sponsorBlock,
            videoCodec
        };

        foreach (var option in options)
            rootCommand.AddOption(option);

        RunOptions o = new()
        {
            Inputs = input,
            Output = output,
            Crf = crf,
            Resolution = resolution,
            AudioBitrate = audioBitrate,
            KeepWatermark = keepWatermark,
            SponsorBlock = sponsorBlock,
            VideoCodec = videoCodec
        };

        return Task.FromResult<(RootCommand, RunOptions)>((rootCommand, o));
    }

    #region Validations
    private void ValidateInputs(OptionResult result)
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

    private void ValidateOutput(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (Directory.Exists(input))
            return;

        _logger.Error("Output directory does not exist");
        Environment.Exit(1);
    }

    private void ValidateCrf(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();
        if (input is >= 0 and <= 63)
            return;

        _logger.Error("CRF value must be between 0 and 63 (Avoid values below 20)");
        Environment.Exit(1);
    }

    private void ValidateAudioBitrate(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();
        if (input % 2 == 0 && input > 0)
            return;

        _logger.Error("Invalid audio bitrate");
        Environment.Exit(1);
    }

    private void ValidateVideoCodec(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (_globals.ValidVideoCodesMap.ContainsKey(input))
            return;

        _logger.Error("Invalid video codec");
        Environment.Exit(1);
    }

    private void ValidateResolution(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (_globals.ResolutionList.Contains(input))
            return;

        _logger.Error("Invalid resolution");
        Environment.Exit(1);
    }

    #endregion Validations
}