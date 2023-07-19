using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public interface IVideoDownloader
{
    Task<string?> Download(IProgress<DownloadProgress> progress);
}