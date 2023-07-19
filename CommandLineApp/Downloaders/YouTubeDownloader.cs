using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

public class YouTubeDownloader : VideoDownloaderBase
{
    private readonly bool _sponsorBlockValue;
    private const string SponsorBlockMessage = "Removing sponsored segments using SponsorBlock";

    public YouTubeDownloader(YoutubeDL youtubeDl, Uri url, bool sponsorBlockValue)
        : base(youtubeDl, url, Serilog.Log.ForContext<YouTubeDownloader>())
    {
        _sponsorBlockValue = sponsorBlockValue;
    }

    public override async Task<string?> Download(IProgress<DownloadProgress> progress)
    {
        OptionSet sponsorOptions = new() { SponsorblockRemove = "all" };
        var overrideOptions = _sponsorBlockValue ? sponsorOptions : null;
        if (overrideOptions is not null)
            Logger.Information(SponsorBlockMessage);
        var fetch = await YoutubeDl.RunVideoDataFetch(Url.ToString());
        if (!fetch.Success)
            return default;
        if (fetch.Data.IsLive is true)
        {
            Logger.Error(LiveStreamError);
            return default;
        }

        var download = await YoutubeDl.RunVideoDownload(Url.ToString(), progress: progress, overrideOptions: overrideOptions);
        if (!download.Success)
        {
            Logger.Error(DownloadError);
            return default;
        }

        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault()!;
        return path;
    }
}