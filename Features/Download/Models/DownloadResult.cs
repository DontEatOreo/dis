using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.Features.Download.Models;

public record DownloadResult(string? OutPath, RunResult<VideoData>? fetchResult);
