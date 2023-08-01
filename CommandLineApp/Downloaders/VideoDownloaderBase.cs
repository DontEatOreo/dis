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
    
    protected readonly Progress<DownloadProgress> DownloadProgress = new(p =>
    {
        var downloadString = p.DownloadSpeed is not null
            ? $"\rDownload Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}"
            : $"\rDownload Progress: {p.Progress:P2}";
        Console.Write(downloadString);
    });

    public abstract Task<string?> Download();
}
