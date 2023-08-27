using dis.CommandLineApp.Models;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class RedditDownloader : VideoDownloaderBase
{
    private const int IdSegmentLength = 4;

    public RedditDownloader(YoutubeDL youtubeDl, DownloadQuery downloadQuery)
        : base(youtubeDl, downloadQuery, Serilog.Log.ForContext<RedditDownloader>()) { }

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

        var split = Query.OptionSet?.DownloadSections.Values.FirstOrDefault();
        if (split is not null)
        {
            var times = ParseStartAndEndTime(split);
            var duration = fetch.Data.Duration;
            if (!AreStartAndEndTimesValid(times, duration))
                return new DownloadResult(null, null);
        }

        var date = fetch.Data.UploadDate ?? fetch.Data.ReleaseDate;
        RunResult<string>? download =
            await YoutubeDl.RunVideoDownload(Query.Uri.ToString(), overrideOptions: Query.OptionSet, progress: DownloadProgress);

        if (download.Success is false)
            return new DownloadResult(null, null);

        var oldId = fetch.Data.ID;
        var tempUri = new Uri(fetch.Data.WebpageUrl);
        var videoId = tempUri.Segments[IdSegmentLength].TrimEnd('/');

        var videoPath = Directory.GetFiles(YoutubeDl.OutputFolder)
            .FirstOrDefault(f => f.Contains(oldId));

        if (File.Exists(videoPath) is false)
            throw new FileNotFoundException($"File {oldId} with ID {videoId} not found");

        var extension = Path.GetExtension(videoPath);
        var destFile = Path.Combine(YoutubeDl.OutputFolder, $"{videoId}{extension}");

        File.Move(videoPath, destFile);
#if DEBUG
        Console.WriteLine();
        Logger.Information("File moved from {VideoPath} to {DestFile}", videoPath, destFile);
#endif

        var path = Directory.GetFiles(YoutubeDl.OutputFolder)
            .FirstOrDefault(f => f.Contains(videoId));

        return new DownloadResult(path, date);
    }
}
