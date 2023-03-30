using Pastel;
using Xabe.FFmpeg;

namespace dis;

public class Converter
{
    #region Constructor

    private readonly Globals _globals;

    private readonly Progress _progress;

    public Converter(Globals globals, Progress progress)
    {
        _globals = globals;
        _progress = progress;
    }

    #endregion

    #region Methods

    public async Task ConvertVideo(string videoFilePath,
        string? resolution,
        bool generateRandomFileName,
        string outputDirectory,
        int crf,
        int audioBitRate,
        string? videoCodec)
    {
        if (!_globals.ResolutionList.Contains(resolution))
            resolution = null;

        var videoCodecEnum = videoCodec is null
            ? VideoCodec.libx264
            : _globals.ValidVideoCodesMap[videoCodec];

        var compressedVideoPath = ReplaceVideoExtension(videoFilePath, videoCodecEnum);

        var uuid = Guid.NewGuid().ToString()[..4];
        var outputFileName = Path.GetFileName(compressedVideoPath);
        if (generateRandomFileName)
            outputFileName = $"{uuid}{Path.GetExtension(compressedVideoPath)}";

        if (File.Exists(Path.Combine(outputDirectory, outputFileName)))
            outputFileName = $"{Path.GetFileNameWithoutExtension(outputFileName)}-{uuid}{Path.GetExtension(compressedVideoPath)}";

        var outputFilePath = Path.Combine(outputDirectory, outputFileName);

        Console.CancelKeyPress += (_, args) =>
        {
            if (args.SpecialKey is not ConsoleSpecialKey.ControlC)
                return;
            if (File.Exists(outputFilePath))
                File.Delete(outputFilePath);
            Directory.Delete(_globals.TempDir, true);
            Console.WriteLine($"{Environment.NewLine}Canceled");
        };

        var mediaInfo = await FFmpeg.GetMediaInfo(videoFilePath);
        var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
        var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

        if (videoStream is null && audioStream is null)
        {
            await Console.Error.WriteLineAsync("There is no video or audio stream in the file".Pastel(ConsoleColor.Red));
            Environment.Exit(1);
        }

        if (videoStream != null && resolution != null)
            SetResolution(videoStream, resolution);

        var conversion = FFmpeg.Conversions.New()
            .SetPreset(ConversionPreset.VerySlow)
            .SetPixelFormat(videoCodecEnum is VideoCodec.libx264 ? PixelFormat.yuv420p : PixelFormat.yuv420p10le)
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

        _progress.ProgressBar(conversion);
        conversion.SetOutput(outputFilePath);
        await conversion.Start();
        Console.WriteLine($"{Environment.NewLine}Converted video saved at: {outputFilePath}");
    }

    private string ReplaceVideoExtension(string videoPath, VideoCodec videoCodec)
    {
        var extension = string.Empty;
        foreach (var item in _globals.ValidVideoExtensionsMap
                     .Where(item => item.Item2 == videoCodec))
        {
            extension = item.Item1;
            break;
        }

        return Path.ChangeExtension(videoPath, extension);
    }

    private static void SetResolution(IVideoStream videoStream, string resolution)
    {
        double width = videoStream.Width;
        double height = videoStream.Height;

        // Parse the resolution input string (remove the "p" suffix)
        var resolutionInt = int.Parse(resolution[..^1]);

        // Calculate the aspect ratio of the input video
        var aspectRatio = width / height;

        // Calculate the output width and height based on the desired resolution and aspect ratio
        var outputWidth = (int)Math.Round(resolutionInt * aspectRatio);
        var outputHeight = resolutionInt;

        // Round the output width and height to even numbers
        outputWidth -= outputWidth % 2;
        outputHeight -= outputHeight % 2;

        videoStream.SetSize(outputWidth, outputHeight);
    }


    private void AddOptimizedFilter(IConversion conversion, IVideoStream videoStream, VideoCodec videoCodec)
    {
        if (videoCodec is VideoCodec.av1)
        {
            conversion.AddParameter(string.Join(" ", _globals.Av1Args));
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
        }
        if (videoCodec is VideoCodec.vp9)
            conversion.AddParameter(string.Join(" ", _globals.Vp9Args));
        videoStream.SetCodec(videoCodec);
    }

    #endregion
}