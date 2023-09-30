using System.CommandLine;

namespace dis.CommandLineApp.Models;

public sealed class UnParsedOptions
{
    public required CliOption<string[]> Inputs { get; init; }
    public required CliOption<string> Output { get; init; }
    public CliOption<string> Resolution { get; init; } = null!;
    public CliOption<string> VideoCodec { get; init; } = null!;
    public CliOption<string> Trim { get; init; } = null!;

    public CliOption<int> MultiThread { get; init; } = null!;
    public CliOption<int> Crf { get; init; } = null!;
    public CliOption<int> AudioBitrate { get; init; } = null!;

    public CliOption<bool> RandomFileName { get; init; } = null!;
    public CliOption<bool> KeepWatermark { get; init; } = null!;
    public CliOption<bool> SponsorBlock { get; init; } = null!;

    public CliOption<bool> Verbose { get; set; } = null!;
}
