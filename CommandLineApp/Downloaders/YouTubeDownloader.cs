using dis.CommandLineApp.Models;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class YouTubeDownloader : VideoDownloaderBase
{
    private const string SponsorBlockMessage = "Removing sponsored segments using SponsorBlock";

    public YouTubeDownloader(YoutubeDL youtubeDl, DownloadQuery downloadQuery)
        : base(youtubeDl, downloadQuery, Serilog.Log.ForContext<YouTubeDownloader>()) { }

    public override async Task<DownloadResult> Download()
    {
        var fetch = await YoutubeDl.RunVideoDataFetch(Query.Uri.ToString());
        if (fetch.Success is false)
            return new DownloadResult(null, null);
        if (fetch.Data.IsLive is true)
        {
            Console.WriteLine();
            Logger.Error(LiveStreamError);
            return new DownloadResult(null, null);
        }

        var emptySections = AreEmptySections(fetch);
        if (emptySections is false)
            return new DownloadResult(null, null);

        var date = fetch.Data.UploadDate ?? fetch.Data.ReleaseDate;

        var download = await YoutubeDl.RunVideoDownload(Query.Uri.ToString(),
            progress: DownloadProgress,
            overrideOptions: Query.OptionSet);

        if (download.Success is false)
        {
            Logger.Error(DownloadError);
            return new DownloadResult(null, null);
        }

        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault()!;
        return new DownloadResult(path, date);
    }
}
