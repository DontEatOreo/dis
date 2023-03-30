using Pastel;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace dis;

public class Downloader
{
    #region Constructor

    private readonly Globals _globals;

    private readonly Progress _progress;

    public Downloader(Globals globals, Progress progress)
    {
        _globals = globals;
        _progress = progress;
    }

    #endregion

    #region Methods

    public async ValueTask<(bool, string? videoId)> DownloadTask(string url,
        bool keepWaterMarkValue,
        bool sponsorBlockValue)
    {
        _globals.YoutubeDl.OutputFolder = _globals.TempDir; // Set the output folder to the temp directory

        RunResult<VideoData> videoInfo = await _globals.YoutubeDl.RunVideoDataFetch(url);
        if (!videoInfo.Success)
        {
            await Console.Error.WriteLineAsync("Failed to fetch video data".Pastel(ConsoleColor.Red));
            return (false, null);
        }

        var videoId = videoInfo.Data.ID;

        var isLive = videoInfo.Data.IsLive ?? false;
        if (isLive)
        {
            await Console.Error.WriteLineAsync("Live streams are not supported".Pastel(ConsoleColor.Red));
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

            await _globals.YoutubeDl.RunVideoDownload(url,
                progress: _progress.YtDlProgress,
                overrideOptions: new OptionSet
                {
                    Format = $"h264_540p_{tikTokValue}-0"
                });
            videoDownload = true;
        }
        else if (url.Contains("youtu") && sponsorBlockValue)
        {
            await _globals.YoutubeDl.RunVideoDownload(url,
                progress: _progress.YtDlProgress,
                overrideOptions: new OptionSet
                {
                    SponsorblockRemove = "all"
                });
            videoDownload = true;
        }
        else
        {
            var runDownload = await _globals.YoutubeDl.RunVideoDownload(url, progress: _progress.YtDlProgress);
            videoDownload = runDownload.Success;
        }

        // New line after the progress bar
        Console.WriteLine();

        // Use reddit post id instead of video id that way you can know from which post the video was downloaded
        if (url.Contains("reddit"))
        {
            var slashIndex = url.IndexOf("/comments/", StringComparison.Ordinal); // Get the index of the first slash
            var endIndex =
                url.IndexOf("/", slashIndex + 10, StringComparison.Ordinal); // Get the index of the second slash
            videoId = url[(slashIndex + 10)..endIndex]; // Get the video id
        }

        if (videoDownload)
            return (true, videoId);

        await Console.Error.WriteLineAsync($"{"There was an error downloading the video".Pastel(ConsoleColor.Red)}");
        return (false, null);
    }

    #endregion
}