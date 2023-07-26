namespace dis.CommandLineApp.Models;

public sealed class ParsedOptions
{
    public string[] Inputs { get; init; } = null!;
    public string Output { get; init; } = null!;
    public int Crf { get; init; }
    public string? Resolution { get; init; }
    public string? VideoCodec { get; init; }
    public int AudioBitrate { get; init; }
    public bool RandomFileName { get; init; }
    public bool KeepWatermark { get; init; }
    public bool SponsorBlock { get; init; }
}
