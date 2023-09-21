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

    /*
     * We want to parse the string as a float but ignore the * at the beginning
     * The * symbol indicates that the start time is relative to the end time
     * For example, *20-30 means 20 seconds before the end to 30 seconds before the end
     */
    private static (float, float) ParseStartAndEndTime(string downloadSection)
    {
        var split = downloadSection.Split('-');

        var start = float.Parse(split[0]
            .Replace("*", string.Empty));
        var end = float.Parse(split[1]);

        return (start, end);
    }

    /// <summary>
    /// Checks if the start and end times are valid given the duration of the video.
    /// </summary>
    /// <param name="start">The start time of the video.</param>
    /// <param name="end">The end time of the video.</param>
    /// <param name="duration">The duration of the video.</param>
    /// <returns>True if the start and end times are valid, false otherwise.</returns>
    private bool IsValidTimeRange(float start, float end, float? duration)
    {
        var startHigherThanEnd = start > end;
        var endHigherThanDuration = end > duration;

        if (startHigherThanEnd || endHigherThanDuration)
        {
            Logger.Error(TrimTimeError);
            return false;
        }

        return true;
    }

    protected bool AreEmptySections(RunResult<VideoData> fetch)
    {
        var emptySections = Query.OptionSet.DownloadSections is null;
        if (emptySections)
            return true;

        var split = Query.OptionSet.DownloadSections!.Values.FirstOrDefault();
        if (split is null)
            return true;

        var (start, end) = ParseStartAndEndTime(split);
        var duration = fetch.Data.Duration;
        return IsValidTimeRange(start, end, duration);
    }

    public abstract Task<DownloadResult> Download();
}
