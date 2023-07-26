using dis.CommandLineApp.Downloaders;
using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Interfaces;

public interface IDownloaderFactory
{
    IVideoDownloader Create(DownloadOptions options);
}
