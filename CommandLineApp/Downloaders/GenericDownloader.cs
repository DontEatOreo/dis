using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class GenericDownloader : VideoDownloaderBase
{
    public GenericDownloader(YoutubeDL youtubeDl, Uri url)
        : base(youtubeDl, url) { }

    public override async Task<string?> Download(IProgress<DownloadProgress> progressCallback)
    {
        var fetch = await YoutubeDl.RunVideoDataFetch(Url.ToString());
        if (!fetch.Success)
            return default;

        await YoutubeDl.RunVideoDownload(Url.ToString());

        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();
        return path ?? default;
    }
}