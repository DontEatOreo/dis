using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Downloaders;

public interface IDownloader
{
    Task<string?> DownloadTask(DownloadOptions options);
}