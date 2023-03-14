using Pastel;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using static dis.Globals;

namespace dis;

public class Downloader
{
    public static async Task<(bool, string? videoId)> DownloadTask(string url,
        bool keepWaterMarkValue,
        bool sponsorBlockValue)
    {
        YoutubeDl.OutputFolder = TempDir; // Set the output folder to the temp directory

        RunResult<VideoData> videoInfo = await YoutubeDl.RunVideoDataFetch(url);
        if (!videoInfo.Success)
        {
            Console.Error.WriteLine("Failed to fetch video data".Pastel(ConsoleColor.Red));
            return (false, null);
        }

        var videoId = videoInfo.Data.ID;

        var isLive = videoInfo.Data.IsLive ?? false;
        if (isLive)
        {
            Console.Error.WriteLine("Live streams are not supported".Pastel(ConsoleColor.Red));
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

            await YoutubeDl.RunVideoDownload(url,
                progress: Progress.YtDlProgress,
                overrideOptions: new YoutubeDLSharp.Options.OptionSet
                {
                    Format = $"h264_540p_{tikTokValue}-0"
                });
            videoDownload = true;
        }
        else if (url.Contains("youtu") && sponsorBlockValue)
        {
            await YoutubeDl.RunVideoDownload(url,
                progress: Progress.YtDlProgress,
                overrideOptions: new YoutubeDLSharp.Options.OptionSet
                {
                    SponsorblockRemove = "all"
                });
            videoDownload = true;
        }
        else
        {
            var runDownload = await YoutubeDl.RunVideoDownload(url, progress: Progress.YtDlProgress);
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
}