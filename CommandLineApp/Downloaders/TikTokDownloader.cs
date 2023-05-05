using System.Text.RegularExpressions;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

/// <summary>
/// Downloader for TikTok videos.
/// </summary>
public partial class TikTokDownloader : VideoDownloaderBase
{
    private readonly bool _keepWaterMarkValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="TikTokDownloader"/> class.
    /// </summary>
    /// <param name="youtubeDl">The YoutubeDL instance to use for downloading.</param>
    /// <param name="url">The URL of the TikTok video to download.</param>
    /// <param name="keepWaterMarkValue">A boolean indicating whether to keep the TikTok watermark or not.</param>
    public TikTokDownloader(YoutubeDL youtubeDl, string url, bool keepWaterMarkValue)
        : base(youtubeDl, url)
    {
        _keepWaterMarkValue = keepWaterMarkValue;
    }

    /// <summary>
    /// Downloads the TikTok video specified by the URL and returns the path of the downloaded video.
    /// </summary>
    /// <param name="progressCallback">The progress callback for reporting the download progress.</param>
    /// <returns>A tuple containing the path of the downloaded video and a boolean indicating if the download was successful.</returns>
    public override async Task<(string path, bool)> Download(IProgress<DownloadProgress> progressCallback)
    {
        var videoInfo = await YoutubeDl.RunVideoDataFetch(Url);
        if (!videoInfo.Success)
        {
            return default;
        }

        var tikTokRegex = TikTokRegex();
        var formatMatch = videoInfo.Data.Formats
            .Select(format => tikTokRegex.Match(format.FormatId))
            .FirstOrDefault(match => match.Success);

        if (formatMatch == null)
        {
            return default;
        }

        var resolution = formatMatch.Groups[1].Value; // e.g. "360p"
        var tikTokValue = formatMatch.Groups[2].Value; // e.g. "400000"
        var format = $"h264_{resolution}_{tikTokValue}-0";

        RunResult<string> runVideoDownload;
        if (_keepWaterMarkValue)
        {
            runVideoDownload = await YoutubeDl.RunVideoDownload(Url,
                progress: progressCallback,
                overrideOptions: new OptionSet
                {
                    Format = format,
                });
        }
        else
        {
            runVideoDownload = await YoutubeDl.RunVideoDownload(Url,
                progress: progressCallback,
                overrideOptions: new OptionSet
                {
                    Format = "download_addr-0",
                });
        }

        // get path of downloaded video
        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault()!;

        return (path, runVideoDownload.Success);
    }

    /// <summary>
    /// Generates the regular expression pattern for matching the TikTok format ID.
    /// </summary>
    /// <returns>A <see cref="Regex"/> object for matching the TikTok format ID.</returns>
    [GeneratedRegex("h264_(\\d+p)_(\\d+)-0")]
    private static partial Regex TikTokRegex();
}