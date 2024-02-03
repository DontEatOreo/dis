using dis.CommandLineApp.Models;
using Spectre.Console;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace dis.CommandLineApp.Downloaders;

public class TwitterDownloader(YoutubeDL youtubeDl, DownloadQuery query) : VideoDownloaderBase(youtubeDl, query)
{
    protected override Task<string> PostDownload(RunResult<VideoData> fetch)
    {
        var videoId = fetch.Data.ID;
        var displayId = fetch.Data.DisplayID;

        var videoPath = Directory.GetFiles(YoutubeDl.OutputFolder)
            .FirstOrDefault(f => f.Contains(videoId));

        if (File.Exists(videoPath) is false)
            throw new FileNotFoundException($"File {videoId} with ID {displayId} not found",
                Path.GetFileName(videoPath));

        var extension = Path.GetExtension(videoPath);
        var destFile = Path.Combine(YoutubeDl.OutputFolder, $"{displayId}{extension}");

        File.Move(videoPath, destFile);

        var path = Directory.GetFiles(YoutubeDl.OutputFolder)
            .FirstOrDefault(f => f.Contains(displayId));
        return Task.FromResult(path ?? string.Empty);
    }
}
