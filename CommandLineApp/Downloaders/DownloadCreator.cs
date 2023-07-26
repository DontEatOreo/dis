using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;
using YoutubeDLSharp;

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
    
    public async Task<string?> DownloadTask(DownloadOptions options)
    {
        PrepareTempDirectory();

        var videoDownloader = _factory.Create(options);
        
        var videoDownload = await videoDownloader.Download(_ytDlProgress);
        if (videoDownload is not null)
            return videoDownload;

        _logger.Error("There was an error downloading the video");
        return default;
    }
    
    private void PrepareTempDirectory()
    {
        var temp = Path.GetTempPath();
        var folderName = Guid.NewGuid().ToString()[..4];
        var tempPath = Path.Combine(temp, folderName);
        _globals.TempDir.Add(tempPath); // We're adding temp path to a global list so after conversion we can delete remaining files.
        _globals.YoutubeDl.OutputFolder = tempPath; // Set temp as output folder
        Directory.CreateDirectory(tempPath);
    }

    private readonly Progress<DownloadProgress> _ytDlProgress = new(p =>
    {
        if (p.Progress is 0)
            return;

        // Write the new progress message
        var downloadString = p.DownloadSpeed is not null
            ? $"\rDownload Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}"
            : $"\rDownload Progress: {p.Progress:P2}";
        Console.Write(downloadString);
    });
}
