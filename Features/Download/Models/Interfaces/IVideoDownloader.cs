namespace dis.Features.Download.Models.Interfaces;

public interface IVideoDownloader
{
    Task<DownloadResult> Download();
}
