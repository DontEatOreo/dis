using dis.CommandLineApp.Models;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class GenericDownloader : VideoDownloaderBase
{
    public GenericDownloader(YoutubeDL youtubeDl, DownloadQuery downloadQuery)
        : base(youtubeDl, downloadQuery, Serilog.Log.ForContext<GenericDownloader>()) { }

    public override async Task<DownloadResult> Download()
    {
        var fetch = await YoutubeDl.RunVideoDataFetch(Query.Uri.ToString());
        if (fetch.Success is false)
            return new DownloadResult(null, null);
        if (fetch.Data.IsLive is true)
        {
            Logger.Error(LiveStreamError);
            return new DownloadResult(null, null);
        }
        var date = fetch.Data.UploadDate ?? fetch.Data.ReleaseDate;

        var download = await YoutubeDl.RunVideoDownload(Query.Uri.ToString(),
            overrideOptions: Query.OptionSet,
            progress: DownloadProgress);
        if (!download.Success)
        {
            Logger.Error(DownloadError);
            return new DownloadResult(null, null);
        }

        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();
        return new DownloadResult(path, date);
    }
}
