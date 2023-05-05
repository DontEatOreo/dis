using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

/// <summary>
/// Downloader for Reddit videos.
/// </summary>
public class RedditDownloader : VideoDownloaderBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedditDownloader"/> class.
    /// </summary>
    /// <param name="youtubeDl">The YoutubeDL instance to use for downloading.</param>
    /// <param name="url">The URL of the Reddit video to download.</param>
    public RedditDownloader(YoutubeDL youtubeDl, string url)
        : base(youtubeDl, url)
    {
    }

    /// <summary>
    /// Downloads the Reddit video specified by the URL and returns the path of the downloaded video.
    /// </summary>
    /// <param name="progressCallback">The progress callback for reporting the download progress.</param>
    /// <returns>A tuple containing the path of the downloaded video and a boolean indicating if the download was successful.</returns>
    public override async Task<(string path, bool)> Download(IProgress<DownloadProgress> progressCallback)
    {
        var runVideoDownload = await YoutubeDl.RunVideoDownload(Url, progress: progressCallback);

        var videoPath = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();
        if (videoPath is null)
        {
            return default;
        }

        var extension = Path.GetExtension(videoPath);

        var slashIndex = Url.IndexOf("/comments/", StringComparison.Ordinal);
        var endIndex = Url.IndexOf("/", slashIndex + 10, StringComparison.Ordinal);
        var videoId = Url[(slashIndex + 10)..endIndex];

        File.Move(videoPath, Path.Combine(YoutubeDl.OutputFolder, $"{videoId}{extension}"));
        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault()!;

        return (path, runVideoDownload.Success);
    }
}