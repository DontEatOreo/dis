using System.ComponentModel;
using Spectre.Console.Cli;

namespace dis.CommandLineApp.Models;

public sealed class Settings : CommandSettings
{
    [Description("Input url/file")]
    [CommandArgument(0, "<input>")]
    [CommandOption("-i|--input")]
    public required string[] Input { get; init; }

    [Description("Output directory")]
    [CommandArgument(0, "<output>")]
    [CommandOption("-o|--output")]
    public string? Output { get; set; }

    [Description("""
                 Constant Rate Factor (CRF)
                 Higher values mean lower quality
                 Lower values mean higher quality
                 You should use a sane value between 22 and 38
                 """)]
    [CommandOption("-c|--crf")]
    [DefaultValue(25)]
    public int Crf { get; init; }

    [CommandOption("-r|--resolution")]
    public string? Resolution { get; init; }

    [Description("""
        Trim input
        Format: ss.ms-ss.ms
        Example: 12.35-67.40
        """)]
    [CommandOption("-t|--trim")]
    public string? Trim { get; init; }

    [Description("Video codec")]
    [CommandOption("--video-codec")]
    public string? VideoCodec { get; init; }

    [Description("Output audio bitrate (in kbit/s)")]
    [CommandOption("--audio-bitrate")]
    public int? AudioBitrate { get; init; }

    [Description("Use all available threads (for faster compression)")]
    [CommandOption("--multi-thread")]
    [DefaultValue(true)]
    public bool MultiThread { get; set; }

    [Description("The output file name will be random file name")]
    [CommandOption("--random")]
    [DefaultValue(false)]
    public bool RandomFileName { get; init; }

    [Description("Keep watermark for TikTok videos")]
    [CommandOption("--keep")]
    [DefaultValue(true)]
    public bool KeepWatermark { get; init; }

    [Description("Remove all sponsor from YouTube Video")]
    [CommandOption("--sponsor")]
    [DefaultValue(false)]
    public bool SponsorBlock { get; init; }
}
