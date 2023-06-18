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
    public RedditDownloader(YoutubeDL youtubeDl, Uri url)
        : base(youtubeDl, url) { }

    /// <summary>
    /// Downloads the Reddit video specified by the URL and returns the path of the downloaded video.
    /// </summary>
    /// <param name="progressCallback">The progress callback for reporting the download progress.</param>
    /// <returns>A tuple containing the path of the downloaded video and a boolean indicating if the download was successful.</returns>
    public override async Task<string?> Download(IProgress<DownloadProgress> progressCallback)
    {
        var fetch = await YoutubeDl.RunVideoDataFetch(Url.ToString());
        if (!fetch.Success)
            return default;

        await YoutubeDl.RunVideoDownload(Url.ToString(), progress: progressCallback);
        Uri uri = new(fetch.Data.WebpageUrl); // Convert to Uri to get segments

        // https://www.reddit.com/r/subreddit/comments/xxxxxxx/title/
        var videoId = uri.Segments[4].TrimEnd('/');

        var oldId = fetch.Data.ID;
        var videoPath = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault(f => f.Contains(oldId))!;
        var extension = Path.GetExtension(videoPath);

        var destFile = Path.Combine(YoutubeDl.OutputFolder, $"{videoId}{extension}");
        File.Move(videoPath, destFile);
        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();

        return path;
    }
}