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

    private const string FormatSort = "vcodec:h264,ext:mp4:m4a";

    private readonly YoutubeDL _youtubeDl;

    public VideoDownloaderFactory(YoutubeDL youtubeDl)
    {
        _youtubeDl = youtubeDl;
    }

    public IVideoDownloader Create(DownloadOptions o)
    {
        Dictionary<string, Func<DownloadQuery, IVideoDownloader>> downloaderDictionary = new()
        {
            { TikTokUrlPart, downloadQuery => new TikTokDownloader(_youtubeDl, downloadQuery, o.KeepWatermark) },
            { YouTubeUrlPart, downloadQuery => new YouTubeDownloader(_youtubeDl, downloadQuery) },
            { RedditUrlPart, downloadQuery => new RedditDownloader(_youtubeDl, downloadQuery) },
        };

        var optionSet = GenerateOptionSet(o);

        DownloadQuery query;
        foreach (var (key, value) in downloaderDictionary)
        {
            if (o.Uri.Host.Contains(key) is false)
                continue;

            query = new DownloadQuery(o.Uri, optionSet);
            return value(query);
        }

        query = new DownloadQuery(o.Uri, optionSet);
        return new GenericDownloader(_youtubeDl, query);
    }

    private static OptionSet GenerateOptionSet(DownloadOptions o)
    {
        var trim = o.Trim;
        var sponsorBlock = o.SponsorBlock;

        OptionSet optionSet = new()
        {
            ForceKeyframesAtCuts = true,
            FormatSort = FormatSort
        };

        if (trim is not null)
        {
            if (trim.Any(char.IsDigit) is false)
                return optionSet;

            var time = trim.Split('-');

            optionSet = new OptionSet
            {
                DownloadSections = $"*{time[0]}-{time[1]}",
            };
        }

        if (sponsorBlock)
            optionSet = new OptionSet
            {
                SponsorblockRemove = "all"
            };

        return optionSet;
    }
}
