using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;
using Spectre.Console;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.CommandLineApp.Downloaders;

public abstract class VideoDownloaderBase(YoutubeDL youtubeDl, DownloadQuery query) : IVideoDownloader
{
    protected readonly YoutubeDL YoutubeDl = youtubeDl;
    protected readonly DownloadQuery Query = query;
    private readonly ILogger _logger = Log.Logger.ForContext<VideoDownloaderBase>();

    private const string LiveStreamError = "Live streams are not supported";
    private const string DownloadError = "Download failed";
    private const string FetchError = "Failed to fetch url";
    private const string TrimTimeError = "Trim time exceeds video length";

    public async Task<DownloadResult> Download()
    {
        var fetch = await FetchVideoData();
        if (fetch is null)
        {
            _logger.Error(FetchError);
            return new DownloadResult(null, null);
        }
        if (fetch.Success is false)
        {
            _logger.Error(FetchError);
            return new DownloadResult(null, null);
        }
        if (fetch.Data.IsLive is true)
        {
            _logger.Error(LiveStreamError);
            return new DownloadResult(null, null);
        }

        var hasKeyframes = Query.OptionSet.ForceKeyframesAtCuts;
        if (hasKeyframes)
        {
            var timeSplit = Query.OptionSet.DownloadSections!.Values[0].Split('-');
            var validTimeRange = ValidTimeRange(timeSplit, fetch.Data.Duration);
            if (validTimeRange is false)
                return new DownloadResult(null, null);
        }

        // Pre-download custom logic
        await PreDownload(fetch);

        var dlResult = await DownloadVideo();
        if (dlResult is null)
            return new DownloadResult(null, null);
        if (dlResult.Success is false)
        {
            _logger.Error(DownloadError);
            return new DownloadResult(null, null);
        }

        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();
        var date = fetch.Data.UploadDate ?? fetch.Data.ReleaseDate;
        return new DownloadResult(path, date);
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

    private async Task<RunResult<string?>?> DownloadVideo()
    {
        RunResult<string?>? download = null;
        await AnsiConsole.Status().StartAsync("Downloading...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Arrow);

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
        // check if the time range is beyond the video length
        var start = float.Parse(timeSplit[0].TrimStart('*'));
        var endStr = timeSplit[1];
        // `inf` in `yt-dlp` means that it will download the video until the end
        var isEndInf = endStr.Equals("inf", StringComparison.InvariantCultureIgnoreCase);
        var end = isEndInf ? float.MaxValue : float.Parse(endStr);

        /*
         * Checks if the start time is less than or equal to the end time,
         * and the end time does not exceed the video's total duration.
         * Valid time range examples:
         * start=0, end=10, duration=20; start=5, end=inf, duration=30
         * Invalid time range examples:
         * start=10, end=5, duration=20; start=0, end=25, duration=20
         */
        var validTimeRange = start <= end && (isEndInf || end <= fetchDuration);

        if (validTimeRange)
            return true;

        _logger.Error(TrimTimeError);
        return false;
    }
}
