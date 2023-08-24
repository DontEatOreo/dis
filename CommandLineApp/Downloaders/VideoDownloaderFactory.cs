using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

public class VideoDownloaderFactory : IDownloaderFactory
{
    // Constants for URL checking
    private const string TikTokUrlPart = "tiktok";
    private const string YouTubeUrlPart = "youtu";
    private const string RedditUrlPart = "redd";

    private readonly YoutubeDL _youtubeDl;

    public VideoDownloaderFactory(YoutubeDL youtubeDl)
    {
        _youtubeDl = youtubeDl;
    }

    public IVideoDownloader Create(DownloadOptions o)
    {
        Dictionary<string,Func<DownloadQuery,IVideoDownloader>> downloaderDictionary = new()
        {
            { TikTokUrlPart, downloadQuery => new TikTokDownloader(_youtubeDl, downloadQuery, o.KeepWatermark) },
            { YouTubeUrlPart, downloadQuery => new YouTubeDownloader(_youtubeDl, downloadQuery) },
            { RedditUrlPart, downloadQuery => new RedditDownloader(_youtubeDl, downloadQuery) }
        };
        
        OptionSet? optionSet = GenerateOptionSet(o.Trim);

        foreach (var downloader in downloaderDictionary)
        {
            if (o.Uri?.Host.Contains(downloader.Key) is not true) 
                continue;
            
            DownloadQuery downloadQuery = new(o.Uri, optionSet);
            return downloader.Value(downloadQuery);
        }

        // Fallback to Generic downloader
        DownloadQuery genralDownloadQuery = new(o.Uri, optionSet);
        return new GenericDownloader(_youtubeDl, genralDownloadQuery);
    }

    private OptionSet? GenerateOptionSet(string? timeOption)
    {
        OptionSet? optionSet = null;
        if (timeOption is null) 
            return optionSet;
        
        var time = timeOption.Split('-');
        optionSet = new OptionSet { ForceKeyframesAtCuts  = true, DownloadSections = $"*{time[0]}-{time[1]}", FormatSort = "vcodec:h264,ext:mp4:m4a" };
        return optionSet;
    }
}
