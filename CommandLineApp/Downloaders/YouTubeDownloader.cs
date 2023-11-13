using dis.CommandLineApp.Models;
using Serilog;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.CommandLineApp.Downloaders;

public class YouTubeDownloader(YoutubeDL youtubeDl, DownloadQuery downloadQuery)
    : VideoDownloaderBase(youtubeDl, downloadQuery)
{
    private const string SponsorBlockMessage = "Removing sponsored segments using SponsorBlock";

    private readonly ILogger _logger = Log.Logger.ForContext<YouTubeDownloader>();

    protected override Task PreDownload(RunResult<VideoData> fetch)
    {
        var sponsorBlockEmpty = string.IsNullOrEmpty(Query.OptionSet.SponsorblockRemove);
        if (sponsorBlockEmpty is false)
            _logger.Information(SponsorBlockMessage);
        return Task.CompletedTask;
    }
}
