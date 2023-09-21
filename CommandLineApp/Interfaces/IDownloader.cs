using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Interfaces;

public interface IDownloader
{
    Task<DownloadResult> DownloadTask(DownloadOptions options);
}
