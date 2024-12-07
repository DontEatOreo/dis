using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.Features.Download.Models.Interfaces;

public interface IDownloader
{
    Task<DownloadResult> DownloadTask(DownloadOptions options, RunResult<VideoData>? fetchResult);
    Task<RunResult<VideoData>?> FetchMetadata(DownloadOptions options);
}
