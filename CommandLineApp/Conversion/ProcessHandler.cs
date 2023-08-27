using dis.CommandLineApp.Models;
using Serilog;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

namespace dis.CommandLineApp.Conversion;

public sealed class ProcessHandler
{
    private readonly CodecParser _codecParser;
    private readonly StreamConfigurator _configurator;
    private readonly ILogger _logger;

    public ProcessHandler(ILogger logger, CodecParser codecParser, StreamConfigurator configurator)
    {
        _logger = logger;
        _codecParser = codecParser;
        _configurator = configurator;
    }

    public void SetTimeStamps(string path, DateTime time)
    {
        File.SetCreationTime(path, time);
        File.SetLastWriteTime(path, time);
        File.SetLastAccessTime(path, time);
    }

    public IConversion? ConfigureConversion(ParsedOptions options, IEnumerable<IStream> streams, string outputPath)
    {
        var listOfStreams = streams.ToList();
        var videoStream = listOfStreams.OfType<IVideoStream>().FirstOrDefault();
        var audioStream = listOfStreams.OfType<IAudioStream>().FirstOrDefault();

        if (videoStream is null && audioStream is null)
        {
            _logger.Error("There is no video or audio stream in the file");
            return default;
        }

        var conversion = FFmpeg.Conversions.New()
            .SetPreset(ConversionPreset.VerySlow)
            .SetPixelFormat(PixelFormat.yuv420p)
            .SetOutput(outputPath)
            .AddParameter($"-crf {options.Crf}");

        if (videoStream is not null)
            conversion.AddStream(videoStream);

        var codecParse = _codecParser.TryParseCodec(options.VideoCodec, out var videoCodec);
        if (codecParse is false)
            videoCodec = VideoCodec.libx264;

        if (videoStream is not null)
        {
            switch (videoCodec)
            {
                case VideoCodec.vp9:
                    {
                        _configurator.SetVp9Args(conversion);
                        break;
                    }
                case VideoCodec.av1:
                    {
                        _configurator.SetCpuForAv1(conversion, videoStream.Framerate);
                        break;
                    }
            }

            if (string.IsNullOrEmpty(options.Resolution) is false)
                _configurator.SetResolution(videoStream, options.Resolution);
        }

        if (audioStream is null)
            return conversion;

        audioStream.SetBitrate(options.AudioBitrate);

        audioStream.SetCodec(videoCodec
            is VideoCodec.vp8 or VideoCodec.vp9 or VideoCodec.av1
            ? AudioCodec.libopus
            : AudioCodec.aac);
        conversion.AddStream(audioStream);

        conversion.OnProgress += ConversionProgress;

        return conversion;
    }

    private void ConversionProgress(object sender, ConversionProgressEventArgs args)
    {
        var percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100);
        if (percent is 0)
            return;

        // Write the new progress message
        var progressMessage = $"\rProgress: {args.Duration.TotalSeconds / args.TotalLength.TotalSeconds:P2}";
        Console.Write(progressMessage);
    }
}