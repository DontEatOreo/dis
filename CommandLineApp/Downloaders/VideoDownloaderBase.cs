using dis.CommandLineApp.Interfaces;
using Serilog;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public abstract class VideoDownloaderBase : IVideoDownloader
{
    protected readonly YoutubeDL YoutubeDl;
    protected readonly Uri Url;
    protected readonly ILogger Logger;
    
    protected const string LiveStreamError = "Live streams are not supported";
    protected const string DownloadError = "Download failed";
    
    protected VideoDownloaderBase(YoutubeDL youtubeDl, Uri url, ILogger logger)
    {
        YoutubeDl = youtubeDl;
        Url = url;
        Logger = logger;
    }

    public abstract Task<string?> Download(IProgress<DownloadProgress> progress);
}