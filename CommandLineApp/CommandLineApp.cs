using dis.CommandLineApp.Conversion;
using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;

namespace dis.CommandLineApp;

public sealed class CommandLineApp : ICommandLineApp
{
    private readonly ILogger _logger;
    private readonly Globals _globals;
    private readonly IDownloader _downloader;
    private readonly Converter _converter;

    public CommandLineApp(ILogger logger,
        Globals globals,
        IDownloader downloader,
        Converter converter)
    {
        _logger = logger;
        _globals = globals;
        _downloader = downloader;
        _converter = converter;
    }

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
        if (list.Any() is false)
            return;

        foreach (var downloadOptions in list.Select(link => new DownloadOptions(link, options)))
        {
            var (path, date) = await _downloader.DownloadTask(downloadOptions);

            if (path is null)
                _logger.Error("There was an error downloading the video");
            else
            {
                var added = videos.TryAdd(path, date);
                if (added is false)
                    _logger.Error("Failed to add video to list: {Path}", path);
            }
        }

        foreach (var path in videos.Keys)
        {
            // Converts the file size to a string with the appropriate unit
            var fileSize = new FileInfo(path).Length;
            var fileSizeStr = fileSize < 1024 * 1024
                ? $"{fileSize / 1024.0:F2} KiB"
                : $"{fileSize / 1024.0 / 1024.0:F2} MiB";

            _logger.Information(
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
                _logger.Verbose("Converting video: {Path}", path);
                await _converter.ConvertVideo(path, date, options);
                _logger.Verbose("Finished converting video: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to convert video: {Path}", path);
            }
        }

        var hasAny = _globals.TempDir.Any();
        if (hasAny is false)
            return;

        _globals.TempDir.ForEach(d =>
        {
            Directory.Delete(d, true);
            _logger.Verbose("Deleted temp dir: {Dir}", d);
        });
    }
}
