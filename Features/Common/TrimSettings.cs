using System.Globalization;

namespace dis.Features.Common;

public record TrimSettings(double Start, double Duration)
{
    // Format for yt-dlp download section (uses seconds)
    public string GetDownloadSection() =>
        $"*{Start.ToString(CultureInfo.InvariantCulture)}-{(Start + Duration).ToString(CultureInfo.InvariantCulture)}";

    // Format for FFmpeg trimming (uses timestamp)
    public string GetFFmpegArgs() =>
        $"-ss {FormatTimeForFFmpeg(TimeSpan.FromSeconds(Start))} -t {FormatTimeForFFmpeg(TimeSpan.FromSeconds(Duration))}";

    private static string FormatTimeForFFmpeg(TimeSpan time) =>
        $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
}
