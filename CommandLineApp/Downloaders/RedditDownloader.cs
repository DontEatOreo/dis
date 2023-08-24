using System.Text.RegularExpressions;
using dis.CommandLineApp.Models;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public partial class RedditDownloader : VideoDownloaderBase
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
        
        var videoIdRegex = RedditIdRegex();
        var videoId = videoIdRegex.Match(Query.Uri.ToString()).Value;
        
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

        return new DownloadResult(path, date);
    }

    /*
     * To get the video ID from the URL, we use a regular expression that matches a specific pattern.
     * The pattern is: (?<=/)[a-zA-Z0-9]{7}
     *
     * This means that we want to find a sequence of 7 alphanumeric characters ([a-zA-Z0-9]{7})
     * that comes right after a slash (/) in the URL. The slash is not part of the match, but it is
     * required to be there. This is achieved by using a positive lookbehind assertion (?<=/), which
     * checks if the preceding characters match a subpattern, but does not include them in the match.
     *
     * For example, in the URL https://www.reddit.com/r/videos/comments/56azx1w/test_title_here/,
     * the video ID is 56azx1w, which matches the pattern because it is 7 alphanumeric characters that
     * come after a slash. The slash is not part of the match, but it is required to be there.
     */
    [GeneratedRegex("(?<=/)[a-zA-Z0-9]{7}", RegexOptions.Compiled)]
    private static partial Regex RedditIdRegex();
}
