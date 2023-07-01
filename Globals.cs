using System.CommandLine;
using Xabe.FFmpeg;
using YoutubeDLSharp;

namespace dis;

public sealed class Globals
{
    public readonly YoutubeDL YoutubeDl = new()
    {
        FFmpegPath = "ffmpeg",
        YoutubeDLPath = "yt-dlp",
        OutputFileTemplate = "%(id)s.%(ext)s",
        OverwriteFiles = false
    };

    public readonly string TempOutputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()[..4]);

    public readonly string[] ResolutionList =
    {
        "144p",
        "240p",
        "360p",
        "480p",
        "720p",
        "1080p",
        "1440p",
        "2160p"
    };

    public readonly string[] Av1Args = {
        "-lag-in-frames 48",
        "-row-mt 1",
        "-tile-rows 0",
        "-tile-columns 1"
    };

    public readonly string[] Vp9Args = {
        "-row-mt 1",
        "-lag-in-frames 25",
        "-cpu-used 4",
        "-auto-alt-ref 1",
        "-arnr-maxframes 7",
        "-arnr-strength 4",
        "-aq-mode 0",
        "-enable-tpl 1",
        "-row-mt 1",
    };

    public readonly Dictionary<string, VideoCodec> ValidVideoCodesMap = new()
    {
        { "h264", VideoCodec.libx264},
        { "libx264", VideoCodec.libx264},
        { "h265", VideoCodec.hevc},
        { "libx265", VideoCodec.hevc},
        { "hevc", VideoCodec.hevc},
        { "vp8", VideoCodec.vp8},
        { "libvpx", VideoCodec.vp8},
        { "vp9", VideoCodec.vp9},
        { "libvpx-vp9", VideoCodec.vp9},
        { "av1", VideoCodec.av1},
        { "libaom-av1", VideoCodec.av1}
    };

    public readonly Dictionary<string, List<VideoCodec>> VideoExtMap = new()
    {
        { "mp4", new List<VideoCodec> { VideoCodec.libx264, VideoCodec.hevc } },
        { "webm", new List<VideoCodec> { VideoCodec.vp8, VideoCodec.vp9, VideoCodec.av1 } }
    };
}

public class RunOptions
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


public class ParsedOptions
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

public class VideoSettings
{
    public string? Resolution { get; set; }
    public bool GenerateRandomFileName { get; init; }
    public string OutputDirectory { get; init; }
    public int Crf { get; init; }
    public int AudioBitRate { get; init; }
    public string? VideoCodec { get; init; }
}

public record DownloadOptions(Uri Uri, bool KeepWatermark, bool SponsorBlock);