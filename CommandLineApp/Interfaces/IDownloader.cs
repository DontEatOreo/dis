using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Interfaces;

public interface IDownloader
{
    Task<string?> DownloadTask(DownloadOptions options);
}
