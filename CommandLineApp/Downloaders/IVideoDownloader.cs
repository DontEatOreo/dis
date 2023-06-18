using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

/// <summary>
/// Interface for all video downloader classes.
/// </summary>
public interface IVideoDownloader
{
    /// <summary>
    /// Downloads the video and returns the path of the downloaded video.
    /// </summary>
    /// <param name="progressCallback">The progress callback for reporting the download progress.</param>
    /// <returns>A tuple containing the path of the downloaded video and a boolean indicating if the download was successful.</returns>
    Task<string?> Download(IProgress<DownloadProgress> progressCallback);
}