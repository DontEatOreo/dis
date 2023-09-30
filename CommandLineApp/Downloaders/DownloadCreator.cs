using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Downloaders;

public sealed class DownloadCreator : IDownloader
{
    private readonly Globals _globals;
    private readonly IDownloaderFactory _factory;

    public DownloadCreator(Globals globals, IDownloaderFactory factory)
    {
        _globals = globals;
        _factory = factory;
    }

    public async Task<DownloadResult> DownloadTask(DownloadOptions options)
    {
        PrepareTempDirectory();

        var videoDownloader = _factory.Create(options);

        var dlResult = await videoDownloader.Download();
        return dlResult.OutPath is null
            ? new DownloadResult(null, null)
            : dlResult;
    }

    /// <summary>
    /// Prepare a TEMP folder for downloading a video
    /// </summary>
    private void PrepareTempDirectory()
    {
        var temp = Path.GetTempPath();
        var folderName = Guid.NewGuid().ToString()[..6];
        var tempPath = Path.Combine(temp, folderName);

        Directory.CreateDirectory(tempPath);

        _globals.YoutubeDl.OutputFolder = tempPath;
        _globals.TempDir.Add(tempPath);
    }
}
