using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.Features.Download.Models.Interfaces;

public interface IVideoDownloader
{
    Task<DownloadResult> Download(RunResult<VideoData>? fetchResult);
}
