using dis.CommandLineApp.Models;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class RedditDownloader : VideoDownloaderBase
{
    public RedditDownloader(YoutubeDL youtubeDl, DownloadQuery downloadQuery)
        : base(youtubeDl, downloadQuery, Serilog.Log.ForContext<RedditDownloader>()) { }

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

        var download = await YoutubeDl.RunVideoDownload(
            url: Query.Uri.ToString(),
            overrideOptions: Query.OptionSet,
            progress: DownloadProgress);

        if (download.Success is false)
            return new DownloadResult(null, null);

        var videoId = fetch.Data.ID;
        var displayId = fetch.Data.DisplayID;

        var videoPath = Directory.GetFiles(YoutubeDl.OutputFolder)
            .FirstOrDefault(f => f.Contains(videoId));

        if (File.Exists(videoPath) is false)
            throw new FileNotFoundException($"File {videoId} with ID {displayId} not found", videoPath);

        var extension = Path.GetExtension(videoPath);
        var destFile = Path.Combine(YoutubeDl.OutputFolder, $"{displayId}{extension}");

        File.Move(videoPath, destFile);

        var path = Directory.GetFiles(YoutubeDl.OutputFolder)
            .FirstOrDefault(f => f.Contains(displayId));

        var date = fetch.Data.UploadDate ?? fetch.Data.ReleaseDate;
        return new DownloadResult(path, date);
    }
}
