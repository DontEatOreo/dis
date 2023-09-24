using System.Text.RegularExpressions;
using dis.CommandLineApp.Models;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

public partial class TikTokDownloader : VideoDownloaderBase
{
    private readonly bool _keepWaterMarkValue;

    public TikTokDownloader(YoutubeDL youtubeDl, DownloadQuery downloadQuery, bool keepWaterMarkValue)
        : base(youtubeDl, downloadQuery, Serilog.Log.ForContext<TikTokDownloader>())
    {
        _keepWaterMarkValue = keepWaterMarkValue;
    }

    public override async Task<DownloadResult> Download()
    {
        var fetch = await YoutubeDl.RunVideoDataFetch(Query.Uri.ToString());
        if (fetch.Success is false)
            return new DownloadResult(null, null);
        if (fetch.Data.IsLive is true)
        {
            Console.WriteLine();
            Logger.Error(LiveStreamError);
            return new DownloadResult(null, null);
        }

        var emptySections = EmptySections(fetch);
        if (emptySections is false)
            return new DownloadResult(null, null);

        var date = fetch.Data.UploadDate ?? fetch.Data.ReleaseDate;

        var tikTokRegex = TikTokRegex();
        var formatMatch = fetch.Data.Formats
            .Select(format => tikTokRegex.Match(format.FormatId))
            .FirstOrDefault(match => match.Success);

        if (formatMatch is null)
            return new DownloadResult(null, null);

        var resolution = formatMatch.Groups[1].Value; // e.g. "360p"
        var tikTokValue = formatMatch.Groups[2].Value; // e.g. "4000000"
        var format = $"h264_{resolution}_{tikTokValue}-0";

        var download = await YoutubeDl.RunVideoDownload(Query.Uri.ToString(),
            progress: DownloadProgress,
            overrideOptions: new OptionSet
            {
                Format = _keepWaterMarkValue ? "download_addr-0" : format
            });

        if (download.Success is false)
        {
            Logger.Error(DownloadError);
            return new DownloadResult(null, null);
        }

        // get path of downloaded video
        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();
        if (path is null)
            throw new InvalidOperationException("No file found in output folder");

        return new DownloadResult(path, date);
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
