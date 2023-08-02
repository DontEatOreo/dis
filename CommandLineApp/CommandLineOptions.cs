using System.CommandLine;
using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;

namespace dis.CommandLineApp;

public sealed class CommandLineOptions
{
    private readonly Globals _globals;
    private readonly ICommandLineValidator _validator;

    public CommandLineOptions(Globals globals, ICommandLineValidator validator)
    {
        _globals = globals;
        _validator = validator;
    }

    public Task<(RootCommand, UnParseOptions)> GetCommandLineOptions()
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
        input.AddValidator(_validator.ValidateInputs);

        string[] outputArr = { "-o", "--output" };
        var outputDescription = $"Directory to save the compressed video to{Environment.NewLine}";
        Option<string> output = new(outputArr, outputDescription);
        output.SetDefaultValue(Environment.CurrentDirectory);
        output.AddValidator(_validator.ValidateOutput);

        string[] videoArr = { "-vc", "--codec", "--video-codec" };
        Option<string> videoCodec = new(videoArr, "Video codec");
        foreach (var key in _globals.ValidVideoCodecsMap.Keys)
            videoCodec.AddCompletions(key);
        videoCodec.AddValidator(_validator.ValidateVideoCodec);

        string[] crfArr = { "-c", "--crf" };
        Option<int> crf = new(crfArr, "CRF value");
        crf.SetDefaultValue(29);
        crf.AddValidator(_validator.ValidateCrf);

        string[] resolutionArr = { "-r", "--resolution" };
        Option<string> resolution = new(resolutionArr, "Resolution");
        resolution.AddCompletions(_globals.ResolutionList);
        resolution.AddValidator(_validator.ValidateResolution);

        string[] audioArr = { "-a", "-ab", "--audio-bitrate" };
        var audioDescription = $"Audio bitrate{Environment.NewLine}Possible values: 32, 64, 96, 128, 192, 256, 320";
        Option<int> audioBitrate = new(audioArr, audioDescription);
        audioBitrate.SetDefaultValue(128);
        audioBitrate.AddValidator(_validator.ValidateAudioBitrate);

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

        UnParseOptions o = new()
        {
            Inputs = input,
            Output = output,
            Crf = crf,
            Resolution = resolution,
            AudioBitrate = audioBitrate,
            KeepWatermark = keepWatermark,
            SponsorBlock = sponsorBlock,
            RandomFileName = randomFileName,
            VideoCodec = videoCodec
        };

        return Task.FromResult<(RootCommand, UnParseOptions)>((rootCommand, o));
    }
}
