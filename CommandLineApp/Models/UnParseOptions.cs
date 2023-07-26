using System.CommandLine;

namespace dis.CommandLineApp.Models;

public sealed class UnParseOptions
{
    public Option<string[]> Inputs { get; init; } = null!;
    public Option<string> Output { get; init; } = null!;
    public Option<int> Crf { get; init; } = null!;
    public Option<string>? Resolution { get; init; }
    public Option<string>? VideoCodec { get; init; }
    public Option<int> AudioBitrate { get; init; } = null!;
    public Option<bool> RandomFilename { get; init; } = null!;
    public Option<bool> KeepWatermark { get; init; } = null!;
    public Option<bool> SponsorBlock { get; init; } = null!;
}
