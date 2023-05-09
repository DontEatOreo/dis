using Serilog;
using Xabe.FFmpeg;

namespace dis;

/// <summary>
/// Converts videos with specified settings.
/// </summary>
public sealed class Converter
{
    private readonly Globals _globals;
    private readonly Progress _progress;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the Converter class.
    /// </summary>
    /// <param name="logger">The logger used for logging.</param>
    /// <param name="globals">The globals used for configuration.</param>
    /// <param name="progress">The progress tracker.</param>
    public Converter(ILogger logger, Globals globals, Progress progress)
    {
        _logger = logger;
        _globals = globals;
        _progress = progress;
    }

    /// <summary>
    /// Converts a video to a specified video settings.
    /// </summary>
    /// <param name="videoFilePath">The path to the video to convert.</param>
    /// <param name="settings">The settings to use for the conversion.</param>
    /// <returns>A task that represents the asynchronous conversion operation.</returns>
    public async Task ConvertVideo(string videoFilePath, VideoSettings settings)
    {
        var videoCodecEnum = settings.VideoCodec is null
            ? VideoCodec.libx264
            : _globals.ValidVideoCodesMap[settings.VideoCodec];

        if (!_globals.ResolutionList.Contains(settings.Resolution))
            settings.Resolution = null;

        var compressedVideoPath = GetCompressedVideoPath(videoFilePath, videoCodecEnum);
        var outputFilePath = ConstructFilePath(settings, compressedVideoPath);

        HandleCancellation(outputFilePath);

        var mediaInfo = await FFmpeg.GetMediaInfo(videoFilePath);

        var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
        var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

        if (videoStream is null || audioStream is null)
        {
            _logger.Error("There is no video or audio stream in the file");
            return;
        }

        if (settings.Resolution != null)
            SetRes(videoStream, settings.Resolution);

        var conversion = ConfigureConversion(settings, videoStream, audioStream);

        _progress.ProgressBar(conversion);
        conversion.SetOutput(outputFilePath);
        await conversion.Start();

        // Converts the file size to a string with the appropriate unit
        var fileSize = new FileInfo(outputFilePath).Length;
        var fileSizeStr = fileSize < 1024 * 1024
            ? $"{fileSize / 1024.0:0.00} KiB"
            : $"{fileSize / 1024.0 / 1024.0:0.00} MiB";

        Console.WriteLine(); // New line after progress bar
        _logger.Information("Converted video saved at: {OutputFilePath} | File Size: {FileSize}", outputFilePath, fileSizeStr);
    }

    private string GetCompressedVideoPath(string videoPath, VideoCodec videoCodec)
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

    private static string ConstructFilePath(VideoSettings settings, string compressedVideoPath)
    {
        var uuid = Guid.NewGuid().ToString()[..4];
        var outputFileName = Path.GetFileName(compressedVideoPath);
        if (settings.GenerateRandomFileName)
            outputFileName = $"{uuid}{Path.GetExtension(compressedVideoPath)}";

        var outputFilePath = Path.Combine(settings.OutputDirectory, outputFileName);

        if (File.Exists(outputFilePath))
            outputFileName = $"{Path.GetFileNameWithoutExtension(outputFileName)}-{uuid}{Path.GetExtension(compressedVideoPath)}";

        return Path.Combine(settings.OutputDirectory, outputFileName);
    }

    private void HandleCancellation(string outputFilePath)
    {
        Console.CancelKeyPress += (_, args) =>
        {
            if (args.SpecialKey is not ConsoleSpecialKey.ControlC)
                return;
            if (File.Exists(outputFilePath))
                File.Delete(outputFilePath);
            Directory.Delete(_globals.TempOutputDir, true);
            _logger.Information("{NewLine}Canceled", Environment.NewLine);
        };
    }

    private static void SetRes(IVideoStream stream, string res)
    {
        double width = stream.Width;
        double height = stream.Height;
        var resInt = int.Parse(res[..^1]);
        var aspectRatio = width / height;
        var outputWidth = (int)Math.Round(resInt * aspectRatio);
        var outputHeight = resInt;
        outputWidth -= outputWidth % 2;
        outputHeight -= outputHeight % 2;
        stream.SetSize(outputWidth, outputHeight);
    }

    private IConversion ConfigureConversion(VideoSettings settings, IVideoStream? videoStream, IAudioStream? audioStream)
    {
        var videoCodecEnum = settings.VideoCodec is null
            ? VideoCodec.libx264
            : _globals.ValidVideoCodesMap[settings.VideoCodec];

        var conversion = FFmpeg.Conversions.New()
            .SetPreset(ConversionPreset.VerySlow)
            .SetPixelFormat(videoCodecEnum is VideoCodec.libx264 ? PixelFormat.yuv420p : PixelFormat.yuv420p10le)
            .AddParameter($"-crf {settings.Crf}");

        if (videoStream != null)
        {
            AddOptimizedFilter(conversion, videoStream, videoCodecEnum);
            conversion.AddStream(videoStream);
        }

        if (audioStream != null)
        {
            audioStream.SetBitrate(settings.AudioBitRate);
            audioStream.SetCodec(videoCodecEnum is VideoCodec.vp8 or VideoCodec.vp9 or VideoCodec.av1
                ? AudioCodec.libopus
                : AudioCodec.aac);
            conversion.AddStream(audioStream);
        }

        return conversion;
    }

    private void AddOptimizedFilter(IConversion conversion, IVideoStream videoStream, VideoCodec videoCodec)
    {
        switch (videoCodec)
        {
            case VideoCodec.av1:
                conversion.AddParameter(string.Join(" ", _globals.Av1Args));
                SetCpuUsedForAv1(conversion, videoStream.Framerate);
                break;
            case VideoCodec.vp9:
                conversion.AddParameter(string.Join(" ", _globals.Vp9Args));
                break;
        }
        videoStream.SetCodec(videoCodec);
    }

    private static void SetCpuUsedForAv1(IConversion conversion, double framerate)
    {
        var cpuUsedParameter = framerate switch
        {
            < 24 => "-cpu-used 2",
            > 60 => "-cpu-used 6",
            _ => "-cpu-used 4"
        };
        conversion.AddParameter(cpuUsedParameter);
    }
}