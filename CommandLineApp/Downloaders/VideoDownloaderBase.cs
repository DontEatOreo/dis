using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.CommandLineApp.Downloaders;

public abstract class VideoDownloaderBase(YoutubeDL youtubeDl, DownloadQuery query) : IVideoDownloader
{
    protected readonly YoutubeDL YoutubeDl = youtubeDl;
    protected readonly DownloadQuery Query = query;
    protected readonly ILogger Logger = Log.Logger.ForContext<VideoDownloaderBase>();

    private const string LiveStreamError = "Live streams are not supported";
    private const string DownloadError = "Download failed";
    private const string FetchError = "Failed to fetch url";
    private const string TrimTimeError = "Trim time exceeds video length";

    public async Task<DownloadResult> Download()
    {
        var fetch = await FetchVideoData();
        if (fetch.Success is false)
        {
            Logger.Error(FetchError);
            return new DownloadResult(null, null);
        }
        if (fetch.Data.IsLive is true)
        {
            Logger.Error(LiveStreamError);
            return new DownloadResult(null, null);
        }

        var validTimeRange = ValidTimeRange(fetch);
        if (validTimeRange is false)
            return new DownloadResult(null, null);

        // Pre-download custom logic
        await PreDownload(fetch);

        // The downloading part can be overridden in child classes
        var dlResult = await DownloadVideo(fetch);
        if (dlResult is null)
            return new DownloadResult(null, null);

        // Post-download custom logic
        var postDownload = await PostDownload(fetch);
        var postNull = string.IsNullOrEmpty(postDownload);

        var path = postNull
            ? Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault()
            : postDownload;
        var date = fetch.Data.UploadDate ?? fetch.Data.ReleaseDate;
        return new DownloadResult(path, date);
    }

    private async Task<RunResult<VideoData>> FetchVideoData()
    {
        Logger.Verbose("Started fetching {QueryUri}", Query.Uri);
        var fetch = await YoutubeDl.RunVideoDataFetch(Query.Uri.ToString());
        Logger.Verbose("Finished fetching {QueryUri}", Query.Uri);
        return fetch;
    }

    protected virtual Task PreDownload(RunResult<VideoData> fetch)
        => Task.CompletedTask;

    protected virtual Task<string> PostDownload(RunResult<VideoData> fetch)
        => Task.FromResult(string.Empty);

    private async Task<RunResult<string?>?> DownloadVideo(RunResult<VideoData> fetch)
    {
        Logger.Verbose("Starting downloading {Title}", fetch.Data.Title);
        var download = await YoutubeDl.RunVideoDownload(Query.Uri.ToString(),
            overrideOptions: Query.OptionSet,
            progress: _downloadProgress);
        Console.WriteLine(); // New line after download progress
        Logger.Verbose("Finished downloading {Title}", fetch.Data.Title);
        if (download.Success)
            return download;

        Logger.Error(DownloadError);
        return default;
    }

    private readonly Progress<DownloadProgress> _downloadProgress = new(p =>
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
    /// Validates if the given time range is valid within the video length.
    /// </summary>
    private bool ValidTimeRange(RunResult<VideoData> fetch)
    {
        // check if the time range is beyond the video length
        var split = Query.OptionSet.DownloadSections!.Values[0];
        var (start, end) = ParseStartAndEndTime(split);

        var duration = fetch.Data.Duration;

        /* The variable 'validTimeRange' checks whether the start time is
         * not greater than the end time, and the end time does not exceed
         * the video's total duration. Therefore, the valid time range for
         * the video is between the start time and the end time, and this
         * range should not be beyond the video's length.
         * The subsequent 'if' condition returns true if both conditions
         * in 'validTimeRange' are satisfied, thus indicating that the
         * defined time range for downloading a section of the video is valid.
         */
        var validTimeRange = start <= end && end <= duration;

        if (validTimeRange)
            return true;

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
         * The '*' character, which is used as a regex symbol for "yt-dlp",
         * it is not relevant for our parsing and is therefore removed.
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
}
