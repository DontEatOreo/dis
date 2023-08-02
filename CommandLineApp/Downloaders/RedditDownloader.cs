using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

public class RedditDownloader : VideoDownloaderBase
{
    private const int IdSegment = 4;

    public RedditDownloader(YoutubeDL youtubeDl, Uri url)
        : base(youtubeDl, url, Serilog.Log.ForContext<RedditDownloader>()) { }

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
        await YoutubeDl.RunVideoDownload(Url.ToString(), progress: DownloadProgress);

        Uri uri = new(fetch.Data.WebpageUrl); // Convert to Uri to get segments
        // "WebPageUrl" will always return this format bellow
        // https://www.reddit.com/r/subreddit/comments/xxxxxxx/title/
        var videoId = uri.Segments[IdSegment].TrimEnd('/');

        var oldId = fetch.Data.ID;

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

        return (path, date);
    }
}
