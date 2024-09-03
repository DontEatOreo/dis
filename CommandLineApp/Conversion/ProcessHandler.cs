using dis.CommandLineApp.Models;
using Serilog;
using Xabe.FFmpeg;

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

    public IConversion? ConfigureConversion(Settings o, IList<IStream> streams, string outP)
    {
        var videoStream = streams.OfType<IVideoStream>().FirstOrDefault();
        var audioStream = streams.OfType<IAudioStream>().FirstOrDefault();

        if (videoStream is null && audioStream is null)
        {
            logger.Error(NoStreamError);
            return default;
        }

        // Optimize mp4 and mov for web playback 
        const string fastStartParm = "-movflags +faststart";
        var crfParm = $"-crf {o.Crf}";

        var parameters = $"{crfParm}";

        var isWebPlayBackFormat = o.VideoCodec is "libx264" or "h264" ||
                                  (bool)o.Output?.Contains("mp4") ||
                                  (bool)o.Output?.Contains("mov");
        if (isWebPlayBackFormat)
        {
            parameters = $"{fastStartParm} {parameters}";
        }
        var conversion = FFmpeg.Conversions.New()
            .AddParameter(parameters)
            .SetPixelFormat(PixelFormat.yuv420p)
            .SetPreset(ConversionPreset.VerySlow);

        var videoCodec = codecParser.GetCodec(o.VideoCodec);

        if (videoStream is not null)
        {
            conversion.AddStream(videoStream);

            switch (videoCodec)
            {
                case VideoCodec.vp9:
                    {
                        configurator.SetVp9Args(conversion);
                        outP = Path.ChangeExtension(outP, "webm");
                        break;
                    }
                case VideoCodec.av1:
                    {
                        configurator.SetCpuForAv1(conversion, videoStream.Framerate);
                        outP = Path.ChangeExtension(outP, "webm");
                        conversion.SetPixelFormat(PixelFormat.yuv420p10le);
                        break;
                    }
                default:
                    {
                        conversion.UseMultiThread(o.MultiThread);
                        break;
                    }
            }

            if (string.IsNullOrEmpty(o.Resolution) is false)
                configurator.SetResolution(videoStream, o.Resolution);
        }

        conversion.SetOutput(outP);

        if (audioStream is null)
            return conversion;

        if (o.AudioBitrate is not null) conversion.SetAudioBitrate((long)(o.AudioBitrate * 1000));

        audioStream.SetCodec(videoCodec
            is VideoCodec.vp8 or VideoCodec.vp9 or VideoCodec.av1
            ? AudioCodec.libopus
            : AudioCodec.aac);
        conversion.AddStream(audioStream);

        return conversion;
    }
}
