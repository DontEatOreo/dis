using dis.CommandLineApp.Conversion;
using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;

namespace dis.CommandLineApp;

public sealed class CommandLineApp(
    ILogger logger,
    Globals globals,
    IDownloader downloader,
    Converter converter)
    : ICommandLineApp
{
    public async Task Handler(ParsedOptions o)
    {
        // On links list we ignore all files by check if they exist
        var links = o.Inputs
            .Where(video =>
                Uri.IsWellFormedUriString(video, UriKind.RelativeOrAbsolute) &&
                File.Exists(video) is false)
            .Select(video => new Uri(video));

        // And now we add them to Separate list
        var files = o.Inputs
            .Where(File.Exists);

        // We store all the downloaded videos here
        Dictionary<string, DateTime?> paths = new();
        await Download(links, paths, o);

        // Add existing files to the videoPaths list
        foreach (var file in files)
            paths.TryAdd(file, null);

        await Convert(paths, o);
    }

    private async Task Download(IEnumerable<Uri> links,
        Dictionary<string, DateTime?> videos,
        ParsedOptions options)
    {
        var list = links.ToList();
        if (list.Count != 0)
            return;

        foreach (var downloadOptions in list.Select(link => new DownloadOptions(link, options)))
        {
            var (path, date) = await downloader.DownloadTask(downloadOptions);

            if (path is null)
                logger.Error("There was an error downloading the video");
            else
            {
                var added = videos.TryAdd(path, date);
                if (added is false)
                    logger.Error("Failed to add video to list: {Path}", path);
            }
        }

        foreach (var path in videos.Keys)
        {
            // Converts the file size to a string with the appropriate unit
            var fileSize = new FileInfo(path).Length;
            var fileSizeStr = fileSize < 1024 * 1024
                ? $"{fileSize / 1024.0:F2} KiB"
                : $"{fileSize / 1024.0 / 1024.0:F2} MiB";

            logger.Information(
                "Downloaded video to: {Path} | Size: {Size}",
                path,
                fileSizeStr);
        }
    }

    private async Task Convert(IEnumerable<KeyValuePair<string, DateTime?>> videos, ParsedOptions options)
    {
        foreach (var (path, date) in videos)
        {
            try
            {
                logger.Verbose("Converting video: {Path}", path);
                await converter.ConvertVideo(path, date, options);
                logger.Verbose("Finished converting video: {Path}", path);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to convert video: {Path}", path);
            }
        }

        var hasAny = globals.TempDir.Count != 0;
        if (hasAny is false)
            return;

        globals.TempDir.ForEach(d =>
        {
            Directory.Delete(d, true);
            logger.Verbose("Deleted temp dir: {Dir}", d);
        });
    }
}
