using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Downloaders;

public interface IDownloaderFactory
{
    IVideoDownloader Create(DownloadOptions options);
}