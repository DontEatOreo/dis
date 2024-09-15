using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;
using Spectre.Console;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

public class VideoDownloaderFactory(YoutubeDL youtubeDl) : IDownloaderFactory
{
    // Constants for URL checking
    private const string YouTubeUrlPart = "youtu";
    private const string FormatSort = "vcodec:h264,ext:mp4:m4a";

    public IVideoDownloader Create(DownloadOptions o)
    {
        Dictionary<string, Func<DownloadQuery, IVideoDownloader>> customDownloadLogicDic = new()
        {
            { YouTubeUrlPart, downloadQuery => new YouTubeDownloader(youtubeDl, downloadQuery) },
        };

        var optionSet = GenerateOptionSet(o);

        var entry = customDownloadLogicDic
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
            FormatSort = FormatSort,
            EmbedMetadata = true
        };

        if (o.Options.SponsorBlock)
            optionSet.SponsorblockRemove = "all";

        var trim = o.Options.Trim;
        if (string.IsNullOrEmpty(trim)) return optionSet;

        // For instance, given the range 2.25-3.00,
        // it will be split into [0] = 2.25 and [1] = 3.00.
        var timeSplit = trim.Split('-');
        if (timeSplit[0].Contains('.') is false)
            timeSplit[0] += ".00";
        if (!timeSplit[1].Contains("inf") && timeSplit[1].Contains('.') is false)
            timeSplit[1] += ".00";

        optionSet.ForceKeyframesAtCuts = true;
        optionSet.DownloadSections = $"*{timeSplit[0]}-{timeSplit[1]}";
        AnsiConsole.MarkupLine($"Trimming video from {timeSplit[0]} to {timeSplit[1]}");

        return optionSet;
    }
}
