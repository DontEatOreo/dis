using dis.Features.Download.Models;
using Spectre.Console;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.Features.Download;

public class YouTubeDownloader(YoutubeDL youtubeDl, DownloadQuery downloadQuery)
    : VideoDownloaderBase(youtubeDl, downloadQuery)
{
    private const string SponsorBlockMessage = "Removing sponsored segments using SponsorBlock";

    protected override Task PreDownload(RunResult<VideoData> fetch)
    {
        var sponsorBlockEmpty = string.IsNullOrEmpty(Query.OptionSet.SponsorblockRemove);
        if (sponsorBlockEmpty is false)
            AnsiConsole.MarkupLine($"[bold green]{SponsorBlockMessage}[/]");
        return Task.CompletedTask;
    }
}
