using System.CommandLine;
using dis.CommandLineApp.Interfaces;

namespace dis.CommandLineApp.Models;

public sealed class UnParsedOptions(ICommandLineValidator validator)
{
    public readonly CliOption<string[]> Inputs = new("input", "-i", "--input")
    {
        Description = "A path to a video link or file",
        AllowMultipleArgumentsPerToken = true,
        Required = true,
        Validators = { validator.Inputs }
    };

    public readonly CliOption<string> Output = new("output", "-o", "--output")
    {
        Description = "Directory to save the compressed video to",
        DefaultValueFactory = _ => Environment.CurrentDirectory,
        Validators = { validator.Output }
    };
    public readonly CliOption<string> Resolution = new("resolution", "-r", "--resolution")
    {
        Description = "Video resolution",
        Validators = { validator.Resolution }
    };
    public readonly CliOption<string> VideoCodec = new("videoCodec", "-vc", "--video-codec")
    {
        Description = "Video codec",
        Validators = { validator.VideoCodec }
    };
    public readonly CliOption<string> Trim = new("trim", "-t", "--trim")
    {
        Description = """
                      Trim the video
                      Format: ss.ms-mm.ms
                      Example: 12.35-67.40
                      """,
        Validators = { validator.Trim }
    };

    public readonly CliOption<int> MultiThread = new("multiThread", "-mt", "--multi-thread")
    {
        Description = "Number of threads to use",
        DefaultValueFactory = _ => Environment.ProcessorCount,
        Validators = { validator.MultiThread }
    };
    public readonly CliOption<int> Crf = new("crf", "-c", "--crf")
    {
        Description = """
                      Constant Rate Factor (CRF)
                      Higher values mean lower quality
                      Lower values mean higher quality
                      You should use a sane value between 22 and 38
                      """,
        DefaultValueFactory = _ => 22,
        Validators = { validator.Crf }
    };
    public readonly CliOption<int> AudioBitrate = new("audioBitrate", "-ab", "--audio-bitrate")
    {
        Description = "Audio bitrate",
        DefaultValueFactory = _ => 128,
        Validators = { validator.AudioBitRate }
    };

    public readonly CliOption<bool> RandomFileName = new("randomFileName", "-rfn", "--random-file-name")
    {
        Description = "The output file name will be random file name",
        DefaultValueFactory = _ => false
    };
    public readonly CliOption<bool> KeepWatermark = new("keepWatermark", "-kw", "--keep-watermark")
    {
        Description = "Keep watermark for TikTok",
        DefaultValueFactory = _ => false
    };
    public readonly CliOption<bool> SponsorBlock = new("sponsorBlock", "-sb", "--sponsor-block")
    {
        Description = "Use sponsor block for YouTube",
        DefaultValueFactory = _ => false
    };

    public readonly CliOption<bool> Verbose = new("verbose", "--verbose")
    {
        Description = "Verbose output",
        DefaultValueFactory = _ => false
    };
}
