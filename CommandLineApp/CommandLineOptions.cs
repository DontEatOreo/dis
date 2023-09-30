using System.CommandLine;
using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;

namespace dis.CommandLineApp;

public sealed class CommandLineOptions : ICommandLineOptions
{
    private readonly ICommandLineValidator _validator;

    public CommandLineOptions(ICommandLineValidator validator)
    {
        _validator = validator;
    }

    public (CliConfiguration, UnParsedOptions) GetCommandLineOptions()
    {
        CliRootCommand rootCommand = new();

        CliOption<bool> randomFileName = new("input", "-rd", "--random")
        {
            Description = "Generate a random file name",
        };

        CliOption<bool> keepWatermark = new("watermark", "-k", "--keep")
        {
            Description = "Keep the watermark",
        };

        CliOption<bool> sponsorBlock = new("sponsorBlock", "-sb", "--sponsor")
        {
            Description = "Remove sponsors segments from the video",
        };

        CliOption<string[]> input = new("input", "-i", "--input")
        {
            Description = "A path to a video link or file",
            AllowMultipleArgumentsPerToken = true,
            Required = true,
            Validators = { _validator.Inputs }
        };

        CliOption<string> output = new("output", "-o", "--output")
        {
            Description = "Directory to save the compressed video to",
            DefaultValueFactory = _ => Environment.CurrentDirectory,
            Validators = { _validator.Output },
        };

        CliOption<string> videoCodec = new("videoCodec", "-vc", "--video-codec")
        {
            Description = "Video codec",
            Validators = { _validator.VideoCodec },
        };

        CliOption<int> multiThread = new("multiThread", "-mt", "--multi-thread")
        {
            Description = "Number of threads to use",
            DefaultValueFactory = _ => 1,
            Validators = { _validator.MultiThread }
        };

        CliOption<int> crf = new("crf", "-c", "--crf")
        {
            Description = "CRF value",
            DefaultValueFactory = _ => 29,
            Validators = { _validator.Crf },
        };

        CliOption<string> resolution = new("resolution", "-r", "--resolution")
        {
            Description = "Resolution",
            Validators = { _validator.Resolution }
        };

        CliOption<string> trim = new("trim", "-t", "--trim")
        {
            Description = "Trim a video from a website",
            Validators = { _validator.Trim }
        };

        CliOption<int> audioBitrate = new("audioBitrate", "-ab", "--audio-bitrate")
        {
            Description = "Audio bitrate",
            Validators = { _validator.AudioBitRate }
        };

        CliOption<bool> verbose = new("verbose", "--verbose")
        {
            Description = "Verbose output",
            DefaultValueFactory = _ => false
        };

        rootCommand.TreatUnmatchedTokensAsErrors = true;

        CliOption[] options =
        {
            randomFileName,
            input,
            output,
            crf,
            resolution,
            audioBitrate,
            keepWatermark,
            sponsorBlock,
            verbose,
            videoCodec,
            trim
        };

        foreach (var option in options)
            rootCommand.Add(option);

        UnParsedOptions o = new()
        {
            Inputs = input,
            Output = output,
            MultiThread = multiThread,
            Crf = crf,
            Resolution = resolution,
            VideoCodec = videoCodec,
            Trim = trim,
            AudioBitrate = audioBitrate,
            RandomFileName = randomFileName,
            KeepWatermark = keepWatermark,
            SponsorBlock = sponsorBlock,
            Verbose = verbose,
        };

        CliConfiguration config = new(rootCommand);

        return (config, o);
    }
}
