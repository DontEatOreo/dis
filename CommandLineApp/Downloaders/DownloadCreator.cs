using dis.CommandLineApp.Models;
using Serilog;

namespace dis.CommandLineApp.Downloaders;


public sealed class Downloader : IDownloader
{
    private readonly Globals _globals;
    private readonly Progress _progress;
    private readonly ILogger _logger;
    private readonly IDownloaderFactory _factory;
    
    public Downloader(Globals globals, Progress progress, ILogger logger, IDownloaderFactory factory)
    {
        _globals = globals;
        _progress = progress;
        _logger = logger;
        _factory = factory;
    }
    
    public async Task<string?> DownloadTask(DownloadOptions options)
    {
        var tempPath = PrepareTempDirectory(options);

        var videoDownloader = _factory.Create(options);
        
        var videoDownload = await videoDownloader.Download(_progress.YtDlProgress);
        if (videoDownload is not null)
            return videoDownload;

        _logger.Error("There was an error downloading the video");
        return default;
    }
    
    private string PrepareTempDirectory(DownloadOptions o)
    {
        var temp = Path.GetTempPath();
        var folderName = Guid.NewGuid().ToString()[..4];
        var tempPath = Path.Combine(temp, folderName);
        _globals.TempDir.Add(tempPath); // We're adding temp path to a global list so after conversion we can delete remaining files.
        _globals.YoutubeDl.OutputFolder = tempPath; // Set temp as output folder
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }
}