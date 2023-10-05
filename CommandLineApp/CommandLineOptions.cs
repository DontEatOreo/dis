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

    public (CliConfiguration, ParsedOptions) GetCommandLineOptions()
    {
        CliRootCommand rootCommand = new()
        {
            TreatUnmatchedTokensAsErrors = true
        };

        UnParsedOptions unParsedOptions = new(_validator);

        CliOption[] options =
        {
            unParsedOptions.Inputs,
            unParsedOptions.Output,
            unParsedOptions.Resolution,
            unParsedOptions.VideoCodec,
            unParsedOptions.Trim,
            unParsedOptions.MultiThread,
            unParsedOptions.Crf,
            unParsedOptions.AudioBitrate,
            unParsedOptions.RandomFileName,
            unParsedOptions.KeepWatermark,
            unParsedOptions.SponsorBlock,
            unParsedOptions.Verbose
        };

        foreach (var option in options)
            rootCommand.Add(option);

        CliConfiguration config = new(rootCommand);
        var configResult = config.Parse(Environment.GetCommandLineArgs());
        var parsedOptions = ParseOptions(configResult, unParsedOptions);

        return (config, parsedOptions);
    }

    private static ParsedOptions ParseOptions(ParseResult result, UnParsedOptions unparsed)
    {
        var inputs = result.GetResult(unparsed.Inputs)?.GetValueOrDefault<string[]>()!;
        var output = result.GetResult(unparsed.Output)?.GetValueOrDefault<string>()!;
        var multiThread = result.GetResult(unparsed.MultiThread)?.GetValueOrDefault<int>() ?? 0;
        var crf = result.GetResult(unparsed.Crf)?.GetValueOrDefault<int>() ?? 0;
        var resolution = result.GetResult(unparsed.Resolution)?.GetValueOrDefault<string>()!;
        var videoCodec = result.GetResult(unparsed.VideoCodec)?.GetValueOrDefault<string>()!;
        var trim = result.GetResult(unparsed.Trim)?.GetValueOrDefault<string>()!;
        var audioBitrate = result.GetResult(unparsed.AudioBitrate)?.GetValueOrDefault<int>() ?? 0;
        var randomFileName = result.GetResult(unparsed.RandomFileName)?.GetValueOrDefault<bool>() ?? false;
        var keepWaterMark = result.GetResult(unparsed.KeepWatermark)?.GetValueOrDefault<bool>() ?? false;
        var sponsorBlock = result.GetResult(unparsed.SponsorBlock)?.GetValueOrDefault<bool>() ?? false;
        var verbose = result.GetResult(unparsed.Verbose)?.GetValueOrDefault<bool>() ?? false;

        ParsedOptions parsed = new()
        {
            Inputs = inputs,
            Output = output,
            MultiThread = multiThread,
            Crf = crf,
            Resolution = resolution,
            VideoCodec = videoCodec,
            Trim = trim,
            AudioBitrate = audioBitrate,
            RandomFileName = randomFileName,
            KeepWatermark = keepWaterMark,
            SponsorBlock = sponsorBlock,
            Verbose = verbose
        };

        return parsed;
    }
}
