using YoutubeDLSharp;

namespace dis.Features.Common;

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
    public readonly List<string> TempDir = [];
}
