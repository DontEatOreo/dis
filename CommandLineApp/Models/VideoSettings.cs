namespace dis.CommandLineApp.Models;

public class VideoSettings
{
    public string? Resolution { get; set; }
    public bool GenerateRandomFileName { get; init; }
    public string OutputDirectory { get; init; }
    public int Crf { get; init; }
    public int AudioBitRate { get; init; }
    public string? VideoCodec { get; init; }
}