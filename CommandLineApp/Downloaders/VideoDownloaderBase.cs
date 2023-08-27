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

    /*
     * We want to parse the string as a float but ignore the * at the beginning
     * The * symbol indicates that the start time is relative to the end time
     * For example, *20-30 means 20 seconds before the end to 30 seconds before the end
     */
    protected Tuple<float?, float?> ParseStartAndEndTime(string downloadSection)
    {
        var split = downloadSection?.Split('-');

        var start = float.TryParse(split?[0].Replace("*", ""), out var result)
            ? result
            : (float?)null;
        var end = float.TryParse(split?[1], out result)
            ? result
            : (float?)null;

        return Tuple.Create(start, end);
    }

    // Checks if values for start and end are within the duration of the video
    // If either is greater than the duration, we return an error
    protected bool AreStartAndEndTimesValid(Tuple<float?, float?> times, float? videoDuration)
    {
        var (start, end) = times;

        if (start is null || end is null)
            return true;
        if (!(start > videoDuration) && !(end > videoDuration))
            return true;

        Logger.Error(TrimTimeError);
        return false;

    }

    public abstract Task<DownloadResult> Download();
}
