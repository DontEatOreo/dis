using System.CommandLine;
using dis.CommandLineApp.Interfaces;

namespace dis.CommandLineApp.Models;

public sealed class UnParsedOptions
{
    public readonly CliOption<string[]> Inputs;

    public readonly CliOption<string> Output;
    public readonly CliOption<string> Resolution;
    public readonly CliOption<string> VideoCodec;
    public readonly CliOption<string> Trim;

    public readonly CliOption<int> MultiThread;
    public readonly CliOption<int> Crf;
    public readonly CliOption<int> AudioBitrate;

    public readonly CliOption<bool> RandomFileName;
    public readonly CliOption<bool> KeepWatermark;
    public readonly CliOption<bool> SponsorBlock;

    public readonly CliOption<bool> Verbose;

    public UnParsedOptions(ICommandLineValidator validator)
    {
        Inputs = new CliOption<string[]>("input", "-i", "--input")
        {
            Description = "A path to a video link or file",
            AllowMultipleArgumentsPerToken = true,
            Required = true,
            Validators = { validator.Inputs }
        };

        Output = new CliOption<string>("output", "-o", "--output")
        {
            Description = "Directory to save the compressed video to",
            DefaultValueFactory = _ => Environment.CurrentDirectory,
            Validators = { validator.Output }
        };

        Resolution = new CliOption<string>("resolution", "-r", "--resolution")
        {
            Description = "Video resolution",
            Validators = { validator.Resolution }
        };

        VideoCodec = new CliOption<string>("videoCodec", "-vc", "--video-codec")
        {
            Description = "Video codec",
            Validators = { validator.VideoCodec }
        };

        Trim = new CliOption<string>("trim", "-t", "--trim")
        {
            Description = """
                          Trim the video
                          Format: ss.ms-mm.ms
                          Example: 12.35-67.40
                          """,
            Validators = { validator.Trim }
        };

        MultiThread = new CliOption<int>("multiThread", "-mt", "--multi-thread")
        {
            Description = "Number of threads to use",
            DefaultValueFactory = _ => Environment.ProcessorCount,
            Validators = { validator.MultiThread }
        };

        Crf = new CliOption<int>("crf", "-c", "--crf")
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

        AudioBitrate = new CliOption<int>("audioBitrate", "-ab", "--audio-bitrate")
        {
            Description = "Audio bitrate",
            DefaultValueFactory = _ => 128,
            Validators = { validator.AudioBitRate }
        };

        RandomFileName = new CliOption<bool>("randomFileName", "-rfn", "--random-file-name")
        {
            Description = "The output file name will be random file name",
            DefaultValueFactory = _ => false
        };

        KeepWatermark = new CliOption<bool>("keepWatermark", "-kw", "--keep-watermark")
        {
            Description = "Keep watermark for TikTok",
            DefaultValueFactory = _ => false
        };

        SponsorBlock = new CliOption<bool>("sponsorBlock", "-sb", "--sponsor-block")
        {
            Description = "Use sponsor block for YouTube",
            DefaultValueFactory = _ => false
        };

        Verbose = new CliOption<bool>("verbose", "--verbose")
        {
            Description = "Verbose output",
            DefaultValueFactory = _ => false
        };
    }
}
