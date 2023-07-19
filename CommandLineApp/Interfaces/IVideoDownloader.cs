using YoutubeDLSharp;

namespace dis.CommandLineApp.Interfaces;

public interface IVideoDownloader
{
    Task<string?> Download(IProgress<DownloadProgress> progress);
}