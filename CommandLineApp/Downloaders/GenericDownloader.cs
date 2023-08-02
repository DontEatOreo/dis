using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class GenericDownloader : VideoDownloaderBase
{
    public GenericDownloader(YoutubeDL youtubeDl, Uri url)
        : base(youtubeDl, url, Serilog.Log.ForContext<GenericDownloader>()) { }

    public override async Task<(string?, DateTime?)> Download()
    {
        var fetch = await YoutubeDl.RunVideoDataFetch(Url.ToString());
        if (fetch.Success is false)
            return default;
        if (fetch.Data.IsLive is true)
        {
            Logger.Error(LiveStreamError);
            return default;
        }
        var date = fetch.Data.UploadDate ?? fetch.Data.ReleaseDate;

        var download = await YoutubeDl.RunVideoDownload(Url.ToString(), progress: DownloadProgress);
        if (!download.Success)
        {
            Logger.Error(DownloadError);
            return default;
        }

        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();
        return (path, date);
    }
}
