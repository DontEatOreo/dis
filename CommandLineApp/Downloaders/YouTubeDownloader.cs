using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

/// <summary>
/// Downloader for YouTube videos.
/// </summary>
public class YouTubeDownloader : VideoDownloaderBase
{
    private readonly bool _sponsorBlockValue;
    private const string SponsorBlockMessage = "Removing sponsored segments using SponsorBlock";

    /// <summary>
    /// Initializes a new instance of the <see cref="YouTubeDownloader"/> class.
    /// </summary>
    /// <param name="youtubeDl">The YoutubeDL instance to use for downloading.</param>
    /// <param name="url">The URL of the YouTube video to download.</param>
    /// <param name="sponsorBlockValue">A boolean indicating whether to remove sponsored segments using SponsorBlock or not.</param>
    public YouTubeDownloader(YoutubeDL youtubeDl, Uri url, bool sponsorBlockValue)
        : base(youtubeDl, url, Serilog.Log.ForContext<YouTubeDownloader>())
    {
        _sponsorBlockValue = sponsorBlockValue;
    }

    /// <summary>
    /// Downloads the YouTube video specified by the URL and returns the path of the downloaded video.
    /// </summary>
    /// <param name="progressCallback">The progress callback for reporting the download progress.</param>
    /// <returns>A tuple containing the path of the downloaded video and a boolean indicating if the download was successful.</returns>
    public override async Task<string?> Download(IProgress<DownloadProgress> progressCallback)
    {
        OptionSet sponsorOptions = new() { SponsorblockRemove = "all" };
        var overrideOptions = _sponsorBlockValue ? sponsorOptions : null;
        if (overrideOptions is not null)
            Logger.Information(SponsorBlockMessage);
        var fetch = await YoutubeDl.RunVideoDataFetch(Url.ToString());
        if (!fetch.Success)
            return default;
        if (fetch.Data.IsLive is true)
        {
            Logger.Error(LiveStreamError);
            return default;
        }

        var download = await YoutubeDl.RunVideoDownload(Url.ToString(), progress: progressCallback, overrideOptions: overrideOptions);
        if (!download.Success)
        {
            Logger.Error(DownloadError);
            return default;
        }

        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault()!;
        return path;
    }
}