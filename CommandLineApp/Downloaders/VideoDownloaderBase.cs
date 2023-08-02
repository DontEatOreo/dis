using dis.CommandLineApp.Interfaces;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
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
        var progress = Math.Round(p.Progress, 2);
        if (progress is 0)
            return;

        var downloadString = p.DownloadSpeed is not null
            ? $"\rDownload Progress: {progress:P2} | Download speed: {p.DownloadSpeed}"
            : $"\rDownload Progress: {progress:P2}";
        Console.Write(downloadString);
    });

    public abstract Task<(string?, DateTime?)> Download();
}
