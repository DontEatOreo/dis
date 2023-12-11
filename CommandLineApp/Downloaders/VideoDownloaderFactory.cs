using System.Text.RegularExpressions;
using dis.CommandLineApp.Interfaces;
using dis.CommandLineApp.Models;
using Serilog;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Downloaders;

public partial class VideoDownloaderFactory(YoutubeDL youtubeDl) : IDownloaderFactory
{
    // Constants for URL checking
    private const string TikTokUrlPart = "tiktok";
    private const string YouTubeUrlPart = "youtu";
    private const string RedditUrlPart = "redd";

    private const string TwitterUrlPart = "twitter";
    private const string XUrlPart = "x.com"; // New Twitter URL

    private const string FormatSort = "vcodec:h264,ext:mp4:m4a";

    public IVideoDownloader Create(DownloadOptions o)
    {
        Dictionary<string, Func<DownloadQuery, IVideoDownloader>> downloaderDictionary = new()
        {
            { TikTokUrlPart, downloadQuery => new TikTokDownloader(youtubeDl, downloadQuery, o.Options.KeepWatermark) },
            { YouTubeUrlPart, downloadQuery => new YouTubeDownloader(youtubeDl, downloadQuery) },
            { RedditUrlPart, downloadQuery => new RedditDownloader(youtubeDl, downloadQuery) },
            { TwitterUrlPart, downloadQuery => new TwitterDownloader(youtubeDl, downloadQuery)},
            { XUrlPart, downloadQuery => new TwitterDownloader(youtubeDl, downloadQuery)}
        };

        var optionSet = GenerateOptionSet(o);

        var entry = downloaderDictionary
            .FirstOrDefault(e => o.Uri.Host.Contains(e.Key));
        var query = new DownloadQuery(o.Uri, optionSet);
        return entry.Key is not null
            ? entry.Value(query)
            : new GenericDownloader(youtubeDl, query);
    }

    private static OptionSet GenerateOptionSet(DownloadOptions o)
    {
        OptionSet optionSet = new()
        {
            FormatSort = FormatSort
        };

        if (o.Options.SponsorBlock)
            optionSet.SponsorblockRemove = "all";

        var trim = o.Options.Trim;
        if (string.IsNullOrEmpty(trim))
        {
            optionSet.EmbedMetadata = true;
            Log.Verbose("EmbedMetadata is set to true");
        }
        else
        {
            /* We simply cannot trust whether a user has correctly used `*`.
             * If they have used it more than once,
             * or have employed it as an index character outside the string,
             * we will just remove it, in the interest of safety.
             */
            if (trim.Contains('*')) trim = trim.Replace("*", "");

            var hasDash = trim.Contains('-');

            if (hasDash is false)
            {
                Log.Error("{OptionsFormat} is not a valid format. Please use the format ss.ms-ss.ms", trim);
                return optionSet;
            }

            var regex = TrimRegex();
            var match = regex.Match(trim);

            if (match.Success is false)
            {
                Log.Error("{OptionsFormat} is not a valid format. Please use the format ss.ms-ss.ms", trim);
                return optionSet;
            }

            // For instance, given the range 2.25-3.00,
            // it will be split into [0] = 2.25 and [1] = 3.00.
            var timeSplit = trim.Split('-');
            if (timeSplit[0].Contains('.') is false)
                timeSplit[0] += ".00";
            if (timeSplit[1].Contains('.') is false)
                timeSplit[1] += ".00";

            optionSet.ForceKeyframesAtCuts = true;
            Log.Verbose("ForceKeyframesAtCuts is set to true");
            optionSet.DownloadSections = $"*{timeSplit[0]}-{timeSplit[1]}";
            Log.Verbose("Trimming video from {Start} to {End}",
                timeSplit[0], timeSplit[1]);
        }

        return optionSet;
    }

    /// <summary>
    /// This regex pattern is used to parse time range strings in 'ss.ms-ss.ms' or 'ss-ss' format.
    /// Mainly used by the 'yt-dlp' '--download-sections' option. Here, 'ss' are one or more digits.
    /// representing seconds, while 'ms' are one or two optional digits for milliseconds.
    /// </summary>
    ///
    /// <remarks>
    /// The pattern breakdown is as follows:
    /// '^' : Starts the pattern.
    /// '\d+' : Matches one or more digits (seconds part).
    /// '(\.\d{1,2})?' : Optionally matches a dot and one or two digits (milliseconds part).
    /// '-' : Matches the dash separating the start and end times.
    /// '\d+' : Matches one or more digits for the end time seconds part.
    /// '(\.\d{1,2})?' : Again, optionally matches a dot and one or two digits for end time milliseconds.
    /// '$' : Ends the pattern.
    ///
    /// If milliseconds part is present, it must have one or two digits only.
    /// </remarks>
    ///
    /// <example>
    /// "222.22-222.22" is valid, having digits, optional dot and one or two digits after dot, separated by a dash.
    /// "222.222-222.222" is invalid, having three digits after the dot is not allowed.
    /// </example>
    [GeneratedRegex(@"^\d+(\.\d{1,2})?-\d+(\.\d{1,2})?$", RegexOptions.Compiled)]
    private static partial Regex TrimRegex();
}
