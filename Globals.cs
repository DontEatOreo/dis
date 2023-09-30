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

    /// <summary>
    /// This list maintains the paths to videos that have been temporarily downloaded.
    /// These paths are stored for future deletion of the corresponding files.
    /// </summary>
    public readonly List<string> TempDir = new();

    public readonly Dictionary<string[], VideoCodec> VideoCodecs = new()
    {
        { new[] { "h264", "libx264" }, VideoCodec.libx264 },
        { new[] { "h265", "libx265", "hevc" }, VideoCodec.hevc },
        { new[] { "vp8", "libvpx" }, VideoCodec.vp8 },
        { new[] { "vp9", "libvpx-vp9" }, VideoCodec.vp9 },
        { new[] { "av1", "libaom-av1" }, VideoCodec.av1 },
    };
}