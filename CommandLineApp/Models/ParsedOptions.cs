namespace dis.CommandLineApp.Models;

public sealed class ParsedOptions
{
    public string[] Inputs { get; init; } = null!;
    public string? Resolution { get; init; }
    public string? VideoCodec { get; init; }
    public string Output { get; init; } = null!;
    public int? Crf { get; init; }
    public int? AudioBitrate { get; init; }
    public bool RandomFileName { get; set; }
    public bool KeepWatermark { get; set; }
    public bool SponsorBlock { get; set; }
}