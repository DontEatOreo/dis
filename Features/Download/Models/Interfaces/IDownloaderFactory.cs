namespace dis.Features.Download.Models.Interfaces;

public interface IDownloaderFactory
{
    IVideoDownloader Create(DownloadOptions options);
}
