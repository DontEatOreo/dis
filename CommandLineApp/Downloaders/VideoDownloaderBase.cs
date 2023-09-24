using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.CommandLineApp.Downloaders;

public abstract class VideoDownloaderBase : IVideoDownloader
{
    protected readonly YoutubeDL YoutubeDl;
    protected readonly DownloadQuery Query;
    protected readonly ILogger Logger;

    protected const string LiveStreamError = "Live streams are not supported";
    protected const string DownloadError = "Download failed";
    private const string TrimTimeError = "Trim time exceeds video length";

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

    /// <summary>
    /// Determines if the sections specified in the OptionSet for downloading are empty.
    /// </summary>
    /// <param name="fetch">The result of the video fetch operation, which also includes the duration.</param>
    /// <returns>True if the sections are empty or invalid; otherwise, false.</returns>
    protected bool EmptySections(RunResult<VideoData> fetch)
    {
        /*
         * For some reason check if DownloadSection (which is MultiValue<string>) is null and we run FirstOrDefault() on it we will get an exception
         * A work around is to use "is null" against it and return early if it's null
         */
        var emptySections = Query.OptionSet.DownloadSections is null;
        if (emptySections)
            return true;

        var split = Query.OptionSet.DownloadSections!.Values[0];
        var (start, end) = ParseStartAndEndTime(split);

        var duration = fetch.Data.Duration;
        var validTimeRange = (start > end || end > duration) is false;
        if (validTimeRange)
            return validTimeRange;

        Logger.Error(TrimTimeError);
        return false;
    }

    /// <summary>
    /// Extracts the start and end times from a given download section string.
    /// </summary>
    /// <param name="downloadSection">The string containing the download section details.</param>
    /// <returns>A tuple containing the start and end times as floats.</returns>
    private static (float, float) ParseStartAndEndTime(string downloadSection)
    {
        /*
         * The download section string is split into two parts using '-' as a separator.
         * The '*' character, which is used as a regex symbol for "yt-dlp", is not relevant for our parsing and is therefore removed.
         * The start time is always on the left side of the split, and the end time is always on the right side.
         */

        var span = downloadSection.AsSpan();
        var separatorIndex = span.IndexOf('-');

        var startSpan = span[..separatorIndex].Trim('*');
        var endSpan = span[(separatorIndex + 1)..];

        var start = float.Parse(startSpan);
        var end = float.Parse(endSpan);

        return (start, end);
    }

    public abstract Task<DownloadResult> Download();
}
