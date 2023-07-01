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

        const string randomDescription = "Randomize the filename";
        string[] randomArr = { "-rn", "-rd", "-rnd", "--random" };
        Option<bool> randomFileName = new(randomArr, randomDescription);
        randomFileName.SetDefaultValue(false);

        string[] watermarkArr = { "-k", "-kw", "-kwm", "--keep" };
        const string watermarkDescription = "Keep the watermark";
        Option<bool> keepWatermark = new(watermarkArr, watermarkDescription);
        keepWatermark.SetDefaultValue(false);

        string[] sponsorArr = { "-sb", "-sponsorblock", "--sponsorblock" };
        const string sponsorDescription = "Remove the sponsorblock from the video";
        Option<bool> sponsorBlock = new(sponsorArr, sponsorDescription);
        keepWatermark.SetDefaultValue(false);

        string[] inputArr = { "-i", "--input", "-f", "--file" };
        const string inputDescription = "A path to a video file or a link to a video";
        Option<string[]> input = new(inputArr, inputDescription)
        {
            AllowMultipleArgumentsPerToken = true,
            IsRequired = true
        };
        input.AddValidator(ValidateInputs);

        string[] outputArr = { "-o", "--output" };
        var outputDescription = $"Directory to save the compressed video to{Environment.NewLine}";
        Option<string> output = new(outputArr, outputDescription);
        output.SetDefaultValue(Environment.CurrentDirectory);
        output.AddValidator(ValidateOutput);

        string[] videoArr = { "-vc", "--codec", "--video-codec" };
        Option<string> videoCodec = new(videoArr, "Video codec");
        foreach (var key in _globals.ValidVideoCodesMap.Keys)
            videoCodec.AddCompletions(key);
        videoCodec.AddValidator(ValidateVideoCodec);

        string[] crfArr = { "-c", "--crf" };
        Option<int> crf = new(crfArr, "CRF value");
        crf.SetDefaultValue(29);
        crf.AddValidator(ValidateCrf);

        string[] resolutionArr = { "-r", "--resolution" };
        Option<string> resolution = new(resolutionArr, "Resolution");
        resolution.AddCompletions(_globals.ResolutionList);
        resolution.AddValidator(ValidateResolution);

        string[] audioArr = { "-a", "-ab", "--audio-bitrate" };
        var audioDescription = $"Audio bitrate{Environment.NewLine}Possible values: 32, 64, 96, 128, 192, 256, 320";
        Option<int> audioBitrate = new(audioArr, audioDescription);
        audioBitrate.SetDefaultValue(128);
        audioBitrate.AddValidator(ValidateAudioBitrate);

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

        const string errorMsg = "Output directory does not exist";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }

    private void ValidateCrf(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();
        if (input is >= 0 and <= 63)
            return;

        const string errorMsg = "CRF value must be between 0 and 63 (Avoid values below 20)";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }

    private void ValidateAudioBitrate(OptionResult result)
    {
        var input = result.GetValueOrDefault<int>();
        if (input % 2 is 0 && input > 0)
            return;

        const string errorMsg = "Audio bitrate must be a multiple of 2";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }

    private void ValidateVideoCodec(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (input is not null)
            if (_globals.ValidVideoCodesMap.ContainsKey(input))
                return;

        const string errorMsg = "Invalid video codec";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }

    private void ValidateResolution(OptionResult result)
    {
        var input = result.GetValueOrDefault<string>();
        if (_globals.ResolutionList.Contains(input))
            return;

        const string errorMsg = "Invalid resolution";
        _logger.Error(errorMsg);
        Environment.Exit(1);
    }
}