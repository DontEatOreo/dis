using dis.Features.Download.Models;
using dis.Features.Download.Models.Interfaces;
using Serilog;
using Spectre.Console;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using dis.Features.Common;

namespace dis.Features.Download;

public abstract class VideoDownloaderBase(YoutubeDL youtubeDl, DownloadQuery query) : IVideoDownloader
{
    protected readonly YoutubeDL YoutubeDl = youtubeDl;
    protected readonly DownloadQuery Query = query;
    private readonly ILogger _logger = Log.Logger.ForContext<VideoDownloaderBase>();
    private TrimSettings? _trimSettings;

    private const string LiveStreamError = "Live streams are not supported";
    private const string DownloadError = "Download failed";
    private const string FetchError = "Failed to fetch url";
    private const string TrimTimeError = "Trim time exceeds video length";

    public async Task<DownloadResult> Download(RunResult<VideoData>? fetchResult)
    {
        var fetch = fetchResult ?? await FetchVideoData();
        if (fetch is null)
        {
            _logger.Error(FetchError);
            return new DownloadResult(null, fetchResult);
        }
        if (fetch.Success is false)
        {
            _logger.Error(FetchError);
            return new DownloadResult(null, fetchResult);
        }
        if (fetch.Data.IsLive is true)
        {
            _logger.Error(LiveStreamError);
            return new DownloadResult(null, fetchResult);
        }

        var hasKeyframes = Query.OptionSet.ForceKeyframesAtCuts;
        if (hasKeyframes)
        {
            var timeSplit = Query.OptionSet.DownloadSections!.Values[0].Split('-');
            var validTimeRange = ValidTimeRange(timeSplit, fetch.Data.Duration);
            if (validTimeRange is false)
                return new DownloadResult(null, fetchResult);
        }

        // Pre-download custom logic
        await PreDownload(fetch);

        // The downloading part can be overridden in child classes
        var dlResult = await DownloadVideo();
        if (dlResult is null)
            return new DownloadResult(null, fetchResult);
        if (dlResult.Success is false)
        {
            _logger.Error(DownloadError);
            return new DownloadResult(null, fetchResult);
        }

        // Post-download custom logic
        var postDownload = await PostDownload(fetch);
        var postNull = string.IsNullOrEmpty(postDownload);

        var path = postNull
            ? Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault()
            : postDownload;
        return new DownloadResult(path, fetchResult);
    }

    private async Task<RunResult<VideoData>?> FetchVideoData()
    {
        RunResult<VideoData>? fetch = null;
        await AnsiConsole.Status().StartAsync("Fetching data...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Arrow);
            fetch = await YoutubeDl.RunVideoDataFetch(Query.Uri.ToString());
            ctx.Refresh();
        });
        return fetch;
    }

    protected virtual Task PreDownload(RunResult<VideoData> fetch)
        => Task.CompletedTask;

    protected virtual Task<string> PostDownload(RunResult<VideoData> fetch)
        => Task.FromResult(string.Empty);

    private async Task<RunResult<string?>?> DownloadVideo()
    {
        RunResult<string?>? download = null;
        await AnsiConsole.Status().StartAsync("Downloading...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Arrow);

            var outputTemplate = _trimSettings != null
                ? $"%(display_id)s-{_trimSettings.GetFilenamePart()}.%(ext)s"
                : "%(display_id)s.%(ext)s";

            Query.OptionSet.Output = Path.Combine(YoutubeDl.OutputFolder, outputTemplate);

            download = await YoutubeDl.RunVideoDownload(Query.Uri.ToString(),
                overrideOptions: Query.OptionSet,
                progress: new Progress<DownloadProgress>(p =>
                {
                    var progress = (int)Math.Round(p.Progress * 100);
                    if (progress is 0 or 1)
                        return;

                    ctx.Status($"[green]Download Progress: {progress}%[/]");
                    ctx.Refresh();
                }));
            return Task.CompletedTask;
        });
        return download;
    }

    /// <summary>
    /// Validates if the given time range is valid within the video length.
    /// </summary>
    private bool ValidTimeRange(string[] timeSplit, float? fetchDuration)
    {
        var start = float.Parse(timeSplit[0].TrimStart('*'));
        var end = float.Parse(timeSplit[1]);

        /*
         * Checks if the start time is less than or equal to the end time,
         * and the end time does not exceed the video's total duration.
         * Valid time range examples:
         * start=0, end=10, duration=20;
         * Invalid time range examples:
         * start=10, end=5, duration=20; start=0, end=25, duration=20
         */
        var validTimeRange = start <= end && end <= (fetchDuration ?? float.MaxValue);

        if (validTimeRange)
        {
            _trimSettings = new TrimSettings(start, end - start);
            Query.OptionSet.Output = $"%(display_id)s-{_trimSettings.GetFilenamePart()}.%(ext)s";
            return true;
        }

        _logger.Error(TrimTimeError);
        return false;
    }
}
