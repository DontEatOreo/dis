using dis.Features.Common;
using dis.Features.Download.Models.Interfaces;

namespace dis.Features.Download.Models;

public sealed class DownloadCreator(Globals globals, IDownloaderFactory factory) : IDownloader
{
    public async Task<DownloadResult> DownloadTask(DownloadOptions options)
    {
        PrepareTempDirectory();

        var videoDownloader = factory.Create(options);

        var dlResult = await videoDownloader.Download();
        return dlResult.OutPath is null
            ? new DownloadResult(null, null)
            : dlResult;
    }

    public async Task<TimeSpan?> GetDuration(DownloadOptions options)
    {
        var result = await globals.YoutubeDl.RunVideoDataFetch(options.Uri.ToString());
        if (result.Success is false)
            throw new Exception();
        if (result.Data.Duration.HasValue is false)
            throw new Exception();
        var duration = TimeSpan.FromSeconds(result.Data.Duration.Value);
        return duration;
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

        globals.YoutubeDl.OutputFolder = tempPath;
        globals.TempDir.Add(tempPath);
    }
}
