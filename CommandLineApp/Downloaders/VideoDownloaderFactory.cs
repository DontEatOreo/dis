using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

public class VideoDownloaderFactory(YoutubeDL youtubeDl) : IDownloaderFactory
{
    // Constants for URL checking
    private const string TikTokUrlPart = "tiktok";
    private const string YouTubeUrlPart = "youtu";
    private const string RedditUrlPart = "redd";

    private const string TwitterUrlPart = "twitter";
    private const string XUrlPart = "x.com"; // New Twitter URL

    private const string FormatSort = "vcodec:h264,ext:mp4:m4a";

    public IVideoDownloader Create(DownloadOptions o)
    {
        Dictionary<string, Func<DownloadQuery, IVideoDownloader>> downloaderDictionary = new()
        {
            { TikTokUrlPart, downloadQuery => new TikTokDownloader(youtubeDl, downloadQuery, o.Options.KeepWatermark) },
            { YouTubeUrlPart, downloadQuery => new YouTubeDownloader(youtubeDl, downloadQuery) },
            { RedditUrlPart, downloadQuery => new RedditDownloader(youtubeDl, downloadQuery) },
            { TwitterUrlPart, downloadQuery => new TwitterDownloader(youtubeDl, downloadQuery)},
            { XUrlPart, downloadQuery => new TwitterDownloader(youtubeDl, downloadQuery)}
        };

        var optionSet = GenerateOptionSet(o);

        var entry = downloaderDictionary
            .FirstOrDefault(e => o.Uri.Host.Contains(e.Key));
        var query = new DownloadQuery(o.Uri, optionSet);
        return entry.Key is not null
            ? entry.Value(query)
            : new GenericDownloader(youtubeDl, query);
    }

    private static OptionSet GenerateOptionSet(DownloadOptions o)
    {
        OptionSet optionSet = new()
        {
            FormatSort = FormatSort
        };

        if (o.Options.Trim is not null)
        {
            if (o.Options.Trim.Any(char.IsDigit) is false)
                return optionSet;

            var time = o.Options.Trim.Split('-');

            optionSet.ForceKeyframesAtCuts = true;
            optionSet.DownloadSections = $"*{time[0]}-{time[1]}";
        }

        if (o.Options.SponsorBlock)
            optionSet.SponsorblockRemove = "all";

        return optionSet;
    }
}
