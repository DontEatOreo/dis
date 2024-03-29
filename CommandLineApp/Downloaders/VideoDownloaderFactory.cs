using System.Text.RegularExpressions;
using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;
using Spectre.Console;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

public partial class VideoDownloaderFactory(YoutubeDL youtubeDl) : IDownloaderFactory
{
    // Constants for URL checking
    private const string TikTokUrlPart = "tiktok";
    private const string YouTubeUrlPart = "youtu";
    private const string RedditUrlPart = "redd";

    private const string TwitterUrlPart = "twitter";
    private const string TwitterCo = "t.co"; // Shortened Twitter URL
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
            { TwitterCo, downloadQuery => new TwitterDownloader(youtubeDl, downloadQuery)},
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

        if (o.Options.SponsorBlock)
            optionSet.SponsorblockRemove = "all";

        var trim = o.Options.Trim;
        if (string.IsNullOrEmpty(trim))
        {
            optionSet.EmbedMetadata = true;
            Log.Verbose("EmbedMetadata is set to true");
        }
        else
        {
            // For instance, given the range 2.25-3.00,
            // it will be split into [0] = 2.25 and [1] = 3.00.
            var timeSplit = trim.Split('-');
            if (timeSplit[0].Contains('.') is false)
                timeSplit[0] += ".00";
            if (timeSplit[1].Contains('.') is false)
                timeSplit[1] += ".00";

            optionSet.ForceKeyframesAtCuts = true;
            optionSet.DownloadSections = $"*{timeSplit[0]}-{timeSplit[1]}";
            AnsiConsole.MarkupLine($"Trimming video from {timeSplit[0]} to {timeSplit[1]}");
        }

        return optionSet;
    }
}
