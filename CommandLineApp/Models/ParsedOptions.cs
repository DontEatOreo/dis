namespace dis.CommandLineApp.Models;

public sealed class ParsedOptions
{
    public required string[] Inputs { get; init; }
    public required string Output { get; init; }
    public required int Crf { get; init; }
    public string? Resolution { get; init; }
    public string? VideoCodec { get; init; }
    
    public string? Trim { get; init; }
    public int AudioBitrate { get; init; }
    public bool RandomFileName { get; init; }
    public bool KeepWatermark { get; init; }
    public bool SponsorBlock { get; init; }
}
