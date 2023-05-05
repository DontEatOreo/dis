using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

/// <summary>
/// Downloader for YouTube videos.
/// </summary>
public class YouTubeDownloader : VideoDownloaderBase
{
    private readonly bool _sponsorBlockValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="YouTubeDownloader"/> class.
    /// </summary>
    /// <param name="youtubeDl">The YoutubeDL instance to use for downloading.</param>
    /// <param name="url">The URL of the YouTube video to download.</param>
    /// <param name="sponsorBlockValue">A boolean indicating whether to remove sponsored segments using SponsorBlock or not.</param>
    public YouTubeDownloader(YoutubeDL youtubeDl, string url, bool sponsorBlockValue)
        : base(youtubeDl, url)
    {
        _sponsorBlockValue = sponsorBlockValue;
    }

    /// <summary>
    /// Downloads the YouTube video specified by the URL and returns the path of the downloaded video.
    /// </summary>
    /// <param name="progressCallback">The progress callback for reporting the download progress.</param>
    /// <returns>A tuple containing the path of the downloaded video and a boolean indicating if the download was successful.</returns>
    public override async Task<(string path, bool)> Download(IProgress<DownloadProgress> progressCallback)
    {
        var overrideOptions = _sponsorBlockValue ? new OptionSet { SponsorblockRemove = "all" } : null;

        var runVideoDownload = await YoutubeDl.RunVideoDownload(Url,
            progress: progressCallback,
            overrideOptions: overrideOptions);

        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault()!;

        return (path, runVideoDownload.Success);
    }
}