namespace dis.Features.Download.Models.Interfaces;

public interface IDownloader
{
    Task<DownloadResult> DownloadTask(DownloadOptions options);
    Task<TimeSpan?> GetDuration(DownloadOptions options);
}
