using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;

namespace dis.CommandLineApp.Downloaders;

public sealed class DownloadCreator : IDownloader
{
    private readonly Globals _globals;
    private readonly ILogger _logger;
    private readonly IDownloaderFactory _factory;

    public DownloadCreator(Globals globals, ILogger logger, IDownloaderFactory factory)
    {
        _globals = globals;
        _logger = logger;
        _factory = factory;
    }

    public async Task<(string?, DateTime?)> DownloadTask(DownloadOptions options)
    {
        PrepareTempDirectory();

        var videoDownloader = _factory.Create(options);

        var (download, date) = await videoDownloader.Download();
        if (download is not null)
            return (download, date);

        _logger.Error("There was an error downloading the video");
        return default;
    }

    // This methods creates a temporary folder in the user's temp directory. To download the video/s
    private void PrepareTempDirectory()
    {
        var temp = Path.GetTempPath();
        var folderName = Guid.NewGuid().ToString()[..6];
        var tempPath = Path.Combine(temp, folderName);

        Directory.CreateDirectory(tempPath);

        _globals.YoutubeDl.OutputFolder = tempPath; // Set temp as output folder
        _globals.TempDir.Add(tempPath); // We're adding temp path to a global list so after conversion we can delete remaining files.
    }
}
