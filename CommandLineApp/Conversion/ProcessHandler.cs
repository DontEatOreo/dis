using dis.CommandLineApp.Models;
using Serilog;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

namespace dis.CommandLineApp.Conversion;

public sealed class ProcessHandler(ILogger logger, CodecParser codecParser, StreamConfigurator configurator)
{
    private const string NoStreamError = "There is no video or audio stream in the file";

    public void SetTimeStamps(string path, DateTime date)
    {
        File.SetCreationTime(path, date);
        File.SetLastWriteTime(path, date);
        File.SetLastAccessTime(path, date);
    }

    public IConversion? ConfigureConversion(ParsedOptions o, IEnumerable<IStream> streams, string outP)
    {
        var listOfStreams = streams.ToList();
        var videoStream = listOfStreams.OfType<IVideoStream>().FirstOrDefault();
        var audioStream = listOfStreams.OfType<IAudioStream>().FirstOrDefault();

        if (videoStream is null && audioStream is null)
        {
            logger.Error(NoStreamError);
            return default;
        }

        var parameters = $"-crf {o.Crf}";
        var conversion = FFmpeg.Conversions.New()
            .SetPreset(ConversionPreset.VerySlow)
            .SetPixelFormat(PixelFormat.yuv420p)
            .SetOutput(outP)
            .UseMultiThread(o.MultiThread)
            .AddParameter(parameters);

        var videoCodec = codecParser.GetCodec(o.VideoCodec);

        if (videoStream is not null)
        {
            conversion.AddStream(videoStream);

            switch (videoCodec)
            {
                case VideoCodec.vp9:
                    configurator.SetVp9Args(conversion);
                    break;
                case VideoCodec.av1:
                    configurator.SetCpuForAv1(conversion, videoStream.Framerate);
                    break;
            }

            if (string.IsNullOrEmpty(o.Resolution) is false)
                configurator.SetResolution(videoStream, o.Resolution);
        }

        conversion.OnProgress += ConversionProgress;

        if (audioStream is null)
            return conversion;

        conversion.SetAudioBitrate(o.AudioBitrate * 1000);

        audioStream.SetCodec(videoCodec
            is VideoCodec.vp8 or VideoCodec.vp9 or VideoCodec.av1
            ? AudioCodec.libopus
            : AudioCodec.aac);
        conversion.AddStream(audioStream);

        return conversion;
    }

    private static void ConversionProgress(object sender, ConversionProgressEventArgs args)
    {
        var percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100);
        if (percent is 0)
            return;

        // Write the new progress message
        var progressMessage = $"\rProgress: {args.Duration.TotalSeconds / args.TotalLength.TotalSeconds:P2}";
        Console.Write(progressMessage);
    }
}
