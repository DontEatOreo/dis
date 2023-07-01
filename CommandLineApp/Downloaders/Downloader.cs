using dis.CommandLineApp.Models;
using Serilog;

namespace dis.CommandLineApp.Downloaders;

/// <summary>
/// The main downloader class that handles downloading of videos from supported platforms.
/// </summary>
public sealed class Downloader
{
    private readonly Globals _globals;
    private readonly Progress _progress;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Downloader"/> class.
    /// </summary>
    /// <param name="globals">The global application options.</param>
    /// <param name="progress">The progress reporting object.</param>
    /// <param name="logger">The logger instance.</param>
    public Downloader(Globals globals, Progress progress, ILogger logger)
    {
        _globals = globals;
        _progress = progress;
        _logger = logger;
    }

    /// <summary>
    /// Downloads a video based on the provided options.
    /// </summary>
    /// <param name="o">The download options.</param>
    /// <returns>A tuple containing the path of the downloaded video and a boolean indicating if the download was successful.</returns>
    public async Task<string?> DownloadTask(DownloadOptions o)
    {
        _globals.YoutubeDl.OutputFolder = _globals.TempOutputDir; // Set the output folder to the temp directory
        Directory.CreateDirectory(_globals.TempOutputDir);

        var videoDownloader = CreateDownloader(o);

        var videoDownload = await videoDownloader.Download(_progress.YtDlProgress);
        if (videoDownload is not null)
            return videoDownload;

        _logger.Error("There was an error downloading the video");
        return default;
    }

    /// <summary>
    /// Creates an instance of a video downloader based on the URL provided in the download options.
    /// </summary>
    /// <param name="o">The download options.</param>
    /// <returns>An instance of a video downloader for the supported platform, or null if the URL is invalid.</returns>
    private IVideoDownloader CreateDownloader(DownloadOptions o)
    {
        return o.Uri switch
        {
            not null when o.Uri.Host.Contains("tiktok") => new TikTokDownloader(_globals.YoutubeDl, o.Uri, o.KeepWatermark),
            not null when o.Uri.Host.Contains("youtu") => new YouTubeDownloader(_globals.YoutubeDl, o.Uri, o.SponsorBlock),
            not null when o.Uri.Host.Contains("reddit") => new RedditDownloader(_globals.YoutubeDl, o.Uri),
            _ => new GenericDownloader(_globals.YoutubeDl, o.Uri!)
        };
    }
}