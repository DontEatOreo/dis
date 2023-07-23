using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class VideoDownloaderFactory : IDownloaderFactory
{
    private readonly YoutubeDL _youtubeDl;

    public VideoDownloaderFactory(YoutubeDL youtubeDl)
    {
        _youtubeDl = youtubeDl;
    }

    public IVideoDownloader Create(DownloadOptions o)
    {
        return o.Uri switch
        {
            { } uri when uri.Host.Contains("tiktok") => new TikTokDownloader(_youtubeDl, uri, o.KeepWatermark),
            { } uri when uri.Host.Contains("youtu") => new YouTubeDownloader(_youtubeDl, uri, o.SponsorBlock),
            { } uri when uri.Host.Contains("reddit") => new RedditDownloader(_youtubeDl, uri),
            _ => new GenericDownloader(_youtubeDl, o.Uri)
        };
    }
}