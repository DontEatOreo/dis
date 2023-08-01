using System.Text.RegularExpressions;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

public partial class TikTokDownloader : VideoDownloaderBase
{
    private readonly bool _keepWaterMarkValue;

    public TikTokDownloader(YoutubeDL youtubeDl, Uri url, bool keepWaterMarkValue)
        : base(youtubeDl, url, Serilog.Log.ForContext<TikTokDownloader>())
    {
        _keepWaterMarkValue = keepWaterMarkValue;
    }

    public override async Task<string?> Download()
    {
        var videoInfo = await YoutubeDl.RunVideoDataFetch(Url.ToString());
        if (!videoInfo.Success)
            return default;
        if (videoInfo.Data.IsLive is true)
        {
            Logger.Error(LiveStreamError);
            return default;
        }

        var tikTokRegex = TikTokRegex();
        var formatMatch = videoInfo.Data.Formats
            .Select(format => tikTokRegex.Match(format.FormatId))
            .FirstOrDefault(match => match.Success);

        if (formatMatch is null)
            return default;

        var resolution = formatMatch.Groups[1].Value; // e.g. "360p"
        var tikTokValue = formatMatch.Groups[2].Value; // e.g. "4000000"
        var format = $"h264_{resolution}_{tikTokValue}-0";

        RunResult<string> download;
        if (_keepWaterMarkValue)
        {
            download = await YoutubeDl.RunVideoDownload(Url.ToString(),
                progress: DownloadProgress,
                overrideOptions: new OptionSet
                {
                    Format = "download_addr-0"
                });
        }
        else
        {
            download = await YoutubeDl.RunVideoDownload(Url.ToString(),
                progress: DownloadProgress,
                overrideOptions: new OptionSet
                {
                    Format = format
                });
        }
        if (!download.Success)
        {
            Logger.Error(DownloadError);
            return default;
        }

        // get path of downloaded video
        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();
        if (path is null)
            throw new InvalidOperationException("No file found in output folder");
        return path;
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
