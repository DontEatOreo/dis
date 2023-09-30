using System.Text.RegularExpressions;
using dis.CommandLineApp.Models;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.CommandLineApp.Downloaders;

public partial class TikTokDownloader : VideoDownloaderBase
{
    private readonly bool _keepWaterMarkValue;

    public TikTokDownloader(YoutubeDL youtubeDl, DownloadQuery downloadQuery, bool keepWaterMarkValue)
        : base(youtubeDl, downloadQuery)
    {
        _keepWaterMarkValue = keepWaterMarkValue;
    }

    protected override Task PreDownload(RunResult<VideoData> fetch)
    {
        var tikTokRegex = TikTokRegex();
        var formatMatch = fetch.Data.Formats
            .Select(format => tikTokRegex.Match(format.FormatId))
            .FirstOrDefault(match => match.Success);
        if (formatMatch is null)
            throw new InvalidOperationException("No TikTok format found");

        var resolution = formatMatch.Groups[1].Value; // e.g. "360p"
        var tikTokValue = formatMatch.Groups[2].Value; // e.g. "4000000"
        var format = $"h264_{resolution}_{tikTokValue}-0";
        Query.OptionSet.Format = _keepWaterMarkValue ? "download_addr-0" : format;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Generates the regular expression pattern for matching the TikTok format ID.
    /// </summary>
    /// <returns>A <see cref="Regex"/> object for matching the TikTok format ID.</returns>
    // string: "h264_720p_1000000-0"
    // We want to parse the resolution and the tiktok value
    [GeneratedRegex(@"^h264_(\d+p)_(\d+)-0$")]
    private static partial Regex TikTokRegex();
}
