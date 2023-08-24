using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public abstract class VideoDownloaderBase : IVideoDownloader
{
    protected readonly YoutubeDL YoutubeDl;
    protected readonly DownloadQuery Query;
    protected readonly ILogger Logger;

    protected const string LiveStreamError = "Live streams are not supported";
    protected const string DownloadError = "Download failed";
    protected const string TrimTimeError = "Trim time exceeds video length";
    
    protected VideoDownloaderBase(YoutubeDL youtubeDl, DownloadQuery query, ILogger logger)
    {
        YoutubeDl = youtubeDl;
        Query = query;
        Logger = logger;
    }

    protected readonly Progress<DownloadProgress> DownloadProgress = new(p =>
    {
        var progress = Math.Round(p.Progress, 2);
        if (progress is 0)
            return;

        var downloadString = p.DownloadSpeed is not null
            ? $"\rDownload Progress: {progress:P2} | Download speed: {p.DownloadSpeed}"
            : $"\rDownload Progress: {progress:P2}";
        Console.Write(downloadString);
    });

    public abstract Task<DownloadResult> Download();
}
