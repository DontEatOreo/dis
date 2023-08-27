using System.CommandLine;

namespace dis.CommandLineApp.Models;

public sealed class UnParseOptions
{
    public required Option<string[]> Inputs { get; init; }
    public required Option<string> Output { get; init; }
    public required Option<int> Crf { get; init; } = null!;
    public Option<string>? Resolution { get; init; }
    public Option<string>? VideoCodec { get; init; }

    public Option<string>? Trim { get; init; }
    public Option<int> AudioBitrate { get; init; } = null!;
    public Option<bool> RandomFileName { get; init; } = null!;
    public Option<bool> KeepWatermark { get; init; } = null!;
    public Option<bool> SponsorBlock { get; init; } = null!;
}
