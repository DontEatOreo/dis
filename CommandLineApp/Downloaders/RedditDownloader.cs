using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class RedditDownloader : VideoDownloaderBase
{
    public RedditDownloader(YoutubeDL youtubeDl, Uri url)
        : base(youtubeDl, url, Serilog.Log.ForContext<RedditDownloader>()) { }

    public override async Task<string?> Download()
    {
        var fetch = await YoutubeDl.RunVideoDataFetch(Url.ToString());
        if (!fetch.Success)
            return default;
        if (fetch.Data.IsLive is true)
        {
            Logger.Error(LiveStreamError);
            return default;
        }

        await YoutubeDl.RunVideoDownload(Url.ToString(), progress: _progress);
        Uri uri = new(fetch.Data.WebpageUrl); // Convert to Uri to get segments

        // https://www.reddit.com/r/subreddit/comments/xxxxxxx/title/
        var videoId = uri.Segments[4].TrimEnd('/');

        var oldId = fetch.Data.ID;
        var videoPath = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault(f => f.Contains(oldId))!;
        var extension = Path.GetExtension(videoPath);

        var destFile = Path.Combine(YoutubeDl.OutputFolder, $"{videoId}{extension}");
        File.Move(videoPath, destFile);
        var path = Directory.GetFiles(YoutubeDl.OutputFolder).FirstOrDefault();

        return path;
    }
    
    private readonly Progress<DownloadProgress> _progress = new(p =>
    {
        var downloadString = p.DownloadSpeed is not null
            ? $"\rDownload Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}"
            : $"\rDownload Progress: {p.Progress:P2}";
        Console.Write(downloadString);
    });
}
