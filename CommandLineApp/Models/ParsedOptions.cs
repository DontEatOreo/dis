namespace dis.CommandLineApp.Models;

public sealed class ParsedOptions
{
    public string[] Inputs { get; init; } = null!;
    public string Output { get; init; } = null!;
    public string? Resolution { get; init; }
    public string? VideoCodec { get; init; }
    public string? Trim { get; init; }

    public int MultiThread { get; init; }
    public int Crf { get; init; }
    public long AudioBitrate { get; init; }

    public bool RandomFileName { get; init; }
    public bool KeepWatermark { get; init; }
    public bool SponsorBlock { get; init; }
    public bool Verbose { get; init; }
}
