using Xabe.FFmpeg;
using static dis.Globals;

namespace dis;

public class Converter
{
    public static async Task ConvertVideo(string videoFilePath,
        string? resolution,
        bool generateRandomFileName,
        string outputDirectory,
        int crf,
        int audioBitRate,
        string? videoCodec)
    {
        if (!ResolutionList.Contains(resolution))
            resolution = null;

        var videoCodecEnum = videoCodec is null
            ? VideoCodec.libx264
            : ValidVideoCodesMap[videoCodec];

        var compressedVideoPath = ReplaceVideoExtension(videoFilePath, videoCodecEnum);

        var uuid = Guid.NewGuid().ToString()[..4];
        var outputFileName = Path.GetFileName(compressedVideoPath);
        if (generateRandomFileName)
            outputFileName = $"{uuid}{Path.GetExtension(compressedVideoPath)}";

        if (File.Exists(Path.Combine(outputDirectory, outputFileName)))
            outputFileName = $"{Path.GetFileNameWithoutExtension(outputFileName)}-{uuid}{Path.GetExtension(compressedVideoPath)}";

        var outputFilePath = Path.Combine(outputDirectory, outputFileName);

        var mediaInfo = await FFmpeg.GetMediaInfo(videoFilePath);
        var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
        var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

        if (videoStream is null && audioStream is null)
        {
            Console.Error.WriteLine("There is no video or audio stream in the file");
            Environment.Exit(1);
        }

        if (videoStream != null && resolution != null)
            SetResolution(videoStream, resolution);

        var conversion = FFmpeg.Conversions.New()
            .SetPreset(ConversionPreset.VerySlow)
            .SetPixelFormat(PixelFormat.yuv420p10le)
            .AddParameter($"-crf {crf}");

        if (videoStream != null)
        {
            AddOptimizedFilter(conversion, videoStream, videoCodecEnum);
            conversion.AddStream(videoStream);
        }

        if (audioStream != null)
        {
            audioStream.SetBitrate(audioBitRate);
            audioStream.SetCodec(videoCodecEnum is VideoCodec.vp8 or VideoCodec.vp9 or VideoCodec.av1
                ? AudioCodec.libopus
                : AudioCodec.aac);
            conversion.AddStream(audioStream);
        }

        Progress.FFmpegProgressBar(conversion);
        conversion.SetOutput(outputFilePath);
        await conversion.Start();
        Console.CancelKeyPress += (_, args) =>
        {
            if (args.SpecialKey is not ConsoleSpecialKey.ControlC)
                return;
            File.Delete(outputFilePath);
            File.Delete(videoFilePath);
            Console.WriteLine("Canceled");
        };
        Console.WriteLine($"Converted video saved at: {outputFilePath}");
    }

    private static string ReplaceVideoExtension(string videoPath, VideoCodec videoCodec)
    {
        var extension = string.Empty;
        foreach (var item in ValidVideoExtensionsMap
                     .Where(item => item.Item2 == videoCodec))
        {
            extension = item.Item1;
            break;
        }

        return Path.ChangeExtension(videoPath, extension);
    }

    private static void SetResolution(IVideoStream videoStream, string resolution)
    {
        double originalWidth = videoStream.Width;
        double originalHeight = videoStream.Height;

        // Parse the resolution input string (remove the "p" suffix)
        var resolutionInt = int.Parse(resolution[..^1]);

        // Calculate the aspect ratio of the input video
        var aspectRatio = originalWidth / originalHeight;

        // Calculate the output width and height based on the desired resolution and aspect ratio
        var outputWidth = (int)Math.Round(resolutionInt * aspectRatio);
        var outputHeight = resolutionInt;

        // Round the output width and height to even numbers
        outputWidth -= outputWidth % 2;
        outputHeight -= outputHeight % 2;

        // Set the output size
        videoStream.SetSize(outputWidth, outputHeight);
    }


    private static void AddOptimizedFilter(IConversion conversion, IVideoStream videoStream, VideoCodec videoCodec)
    {
        switch (videoCodec)
        {
            case VideoCodec.av1:
            {
                conversion.AddParameter(string.Join(" ", Av1Args));
                {
                    switch (videoStream.Framerate)
                    {
                        case < 24:
                            conversion.AddParameter("-cpu-used 2");
                            break;
                        case > 60:
                            conversion.AddParameter("-cpu-used 6");
                            break;
                        case > 30 and < 60:
                            conversion.AddParameter("-cpu-used 4");
                            break;
                    }
                    break;
                }
            }
            case VideoCodec.vp9:
                conversion.AddParameter(string.Join(" ", Vp9Args));
                break;
        }
        videoStream.SetCodec(videoCodec);
    }
}