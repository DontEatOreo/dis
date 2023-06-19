using Serilog;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

/// <summary>
/// Base class for all video downloader classes.
/// </summary>
public abstract class VideoDownloaderBase : IVideoDownloader
{
    protected readonly YoutubeDL YoutubeDl;
    protected readonly Uri Url;
    protected readonly ILogger Logger;
    protected const string LiveStreamError = "Live streams are not supported";
    protected const string DownloadError = "Download failed";

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoDownloaderBase"/> class.
    /// </summary>
    /// <param name="youtubeDl">The YoutubeDL instance to use for downloading.</param>
    /// <param name="url">The URL of the video to download.</param>
    /// <param name="logger">The logger instance.</param>
    protected VideoDownloaderBase(YoutubeDL youtubeDl, Uri url, ILogger logger)
    {
        YoutubeDl = youtubeDl;
        Url = url;
        Logger = logger;
    }

    /// <summary>
    /// Downloads the video specified by the URL and returns the path of the downloaded video.
    /// </summary>
    /// <param name="progressCallback">The progress callback for reporting the download progress.</param>
    /// <returns>A tuple containing the path of the downloaded video and a boolean indicating if the download was successful.</returns>
    public abstract Task<string?> Download(IProgress<DownloadProgress> progressCallback);
}