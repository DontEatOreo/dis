using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace dis;

public class Downloader
{
    #region Ctor

    private readonly Globals _globals;

    private readonly Progress _progress;

    private readonly Serilog.ILogger _logger;

    public Downloader(Globals globals, Progress progress, Serilog.ILogger logger)
    {
        _globals = globals;
        _progress = progress;
        _logger = logger;
    }

    #endregion Ctor

    #region Methods

    public async ValueTask<(bool, string? videoId)> DownloadTask(string url,
        bool keepWaterMarkValue,
        bool sponsorBlockValue)
    {
        _globals.YoutubeDl.OutputFolder = _globals.TempDir; // Set the output folder to the temp directory

        RunResult<VideoData> videoInfo = await _globals.YoutubeDl.RunVideoDataFetch(url).ConfigureAwait(false);
        if (!videoInfo.Success)
        {
            _logger.Error("Failed to fetch video data");
            return (false, null);
        }

        var videoId = videoInfo.Data.ID;

        var isLive = videoInfo.Data.IsLive ?? false;
        url = videoInfo.Data.WebpageUrl;
        if (isLive)
        {
            _logger.Error("Live streams are not supported");
            return (false, null);
        }

        /*
         * Previously you could've download TikTok videos without water mark just by using the "download_addr-2".
         * But now TikTok has changed the format id to "h264_540p_randomNumber-0" so we need to get the random number
         */

        bool videoDownload;
        if (url.Contains("tiktok") && keepWaterMarkValue)
        {
            var tikTokValue = videoInfo.Data.Formats
                .Where(format => !string.IsNullOrEmpty(format.FormatId) && format.FormatId.Contains("h264_540p_"))
                .Select(format => format.FormatId.Split('_').Last().Split('-').First())
                .FirstOrDefault();

            var runVideoDownload = await _globals.YoutubeDl.RunVideoDownload(url,
                progress: _progress.YtDlProgress,
                overrideOptions: new OptionSet
                {
                    Format = $"h264_540p_{tikTokValue}-0"
                });
            videoDownload = runVideoDownload.Success;
        }
        else if (url.Contains("youtu") && sponsorBlockValue)
        {
            var runDownload =
                await _globals.YoutubeDl.RunVideoDownload(url,
                progress: _progress.YtDlProgress,
                overrideOptions: new OptionSet
                {
                    SponsorblockRemove = "all"
                });
            videoDownload = runDownload.Success;
        }
        else if (url.Contains("reddit"))
        {
            var runDownload = await _globals.YoutubeDl.RunVideoDownload(url,
                    progress: _progress.YtDlProgress);

            var videoPath = Directory.GetFiles(_globals.TempDir).FirstOrDefault();
            if (videoPath is null)
                return default;
            var extension = Path.GetExtension(videoPath);

            var slashIndex = url.IndexOf("/comments/", StringComparison.Ordinal); // Get the index of the first slash
            var endIndex =
                url.IndexOf("/", slashIndex + 10, StringComparison.Ordinal); // Get the index of the second slash
            videoId = url[(slashIndex + 10)..endIndex]; // Get the video id

            File.Move(videoPath, Path.Combine(_globals.TempDir, $"{videoId}{extension}")); // Rename the file

            videoDownload = runDownload.Success;
        }
        else
        {
            var runDownload = await _globals.YoutubeDl.RunVideoDownload(url,
                    progress: _progress.YtDlProgress);
            videoDownload = runDownload.Success;
        }

        // New line after the progress bar
        Console.WriteLine();

        if (videoDownload)
            return (true, videoId);

        _logger.Error("There was an error downloading the video");
        return (false, null);
    }

    #endregion
}