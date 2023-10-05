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

    private const string LiveStreamError = "Live streams are not supported";
    private const string DownloadError = "Download failed";
    private const string FetchError = "Failed to fetch url";
    private const string TrimTimeError = "Trim time exceeds video length";

    protected VideoDownloaderBase(YoutubeDL youtubeDl, DownloadQuery query)
    {
        YoutubeDl = youtubeDl;
        Query = query;
        Logger = Log.Logger.ForContext<VideoDownloaderBase>();
    }

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

        var emptySections = EmptySections(fetch);
        if (emptySections is false)
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
    /// Determines if the sections specified in the OptionSet for downloading are empty.
    /// </summary>
    /// <param name="fetch">The result of the video fetch operation, which also includes the duration.</param>
    /// <returns>True if the sections are empty or invalid; otherwise, false.</returns>
    private bool EmptySections(RunResult<VideoData> fetch)
    {
        /*
         * For some reason check if DownloadSection (which is MultiValue<string>)
         * is null and we run FirstOrDefault() on it we will get an exception
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
