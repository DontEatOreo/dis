using System.CommandLine;

namespace dis.CommandLineApp.Models;

public sealed class RunOptions
{
    public Option<string[]> Inputs { get; init; }
    public Option<string> Output { get; init; }
    public Option<string>? Resolution { get; init; }
    public Option<string>? VideoCodec { get; init; }
    public Option<int> Crf { get; init; }
    public Option<int> AudioBitrate { get; init; }
    public Option<bool>? RandomFilename { get; set; }
    public Option<bool>? KeepWatermark { get; init; }
    public Option<bool>? SponsorBlock { get; init; }
}