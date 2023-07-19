using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class GenericDownloader : VideoDownloaderBase
{
    public GenericDownloader(YoutubeDL youtubeDl, Uri url)
        : base(youtubeDl, url, Serilog.Log.ForContext<GenericDownloader>()) { }

    public override async Task<string?> Download(IProgress<DownloadProgress> progress)
    {
        var fetch = await YoutubeDl.RunVideoDataFetch(Url.ToString());
        if (!fetch.Success)
            return default;
        if (fetch.Data.IsLive is true)
        {
            Logger.Error(LiveStreamError);
            return default;
        }

        var download = await YoutubeDl.RunVideoDownload(Url.ToString());
        if (!download.Success)
        {
            Logger.Error(DownloadError);
            return default;
        }

        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();
        return path ?? default;
    }
}