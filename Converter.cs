using dis.CommandLineApp.Models;
using Serilog;
using Xabe.FFmpeg;

namespace dis;

public sealed class Converter
{
    private readonly Globals _globals;
    private readonly Progress _progress;
    private readonly ILogger _logger;

    public Converter(ILogger logger, Globals globals, Progress progress)
    {
        _logger = logger;
        _globals = globals;
        _progress = progress;
    }

    public async Task ConvertVideo(string videoFilePath, VideoSettings settings)
    {
        var selectedCodec = VideoCodec._012v; // dummy value
        foreach(var (key, value) in _globals.ValidVideoCodecsMap)
        {
            if (!key.Contains(settings.VideoCodec)) 
                continue;
            
            selectedCodec = value;
            break;
        }

        var videoCodecEnum = settings.VideoCodec is null
            ? VideoCodec.libx264
            : selectedCodec;

        if (!_globals.ResolutionList.Contains(settings.Resolution))
            settings.Resolution = null;

        var compressedVideoPath = GetCompressedVideoPath(videoFilePath, videoCodecEnum);
        var outputFilePath = ConstructFilePath(settings, compressedVideoPath);

        HandleCancellation(outputFilePath);

        var mediaInfo = await FFmpeg.GetMediaInfo(videoFilePath);

        var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
        var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

        if (videoStream is null)
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
        _logger.Information("Converted video saved at: {OutputFilePath} | Size: {FileSize}",
            outputFilePath,
            fileSizeStr);
    }

    private string GetCompressedVideoPath(string videoPath, VideoCodec videoCodec)
    {
        var videoExtMap = _globals.VideoExtMap;
        string? extension = null;
        foreach (var kvp in videoExtMap)
        {
            if (!kvp.Value.Contains(videoCodec)) 
                continue;
            
            extension = kvp.Key;
            return Path.ChangeExtension(videoPath, extension);
        }

        return Path.ChangeExtension(videoPath, extension);
    }

    private static string ConstructFilePath(VideoSettings settings, string? compressedVideoPath)
    {
        var uuid = Guid.NewGuid().ToString()[..4];
        
        var originalFileName = Path.GetFileName(compressedVideoPath);
        if (originalFileName is null)
            throw new Exception("Could not get the original file name");
        
        var outputFilePath = Path.Combine(settings.OutputDirectory, originalFileName);
        var originalFileExtension = Path.GetExtension(compressedVideoPath);

        var outputFileName = File.Exists(outputFilePath)
            ? $"{Path.GetFileNameWithoutExtension(originalFileName)}-{uuid}{originalFileExtension}"
            : originalFileName;

        if (settings.GenerateRandomFileName)
            outputFileName = $"{uuid}{originalFileExtension}";

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
            _globals.DeleteLeftOvers();
            Console.WriteLine();
            _logger.Information("Canceled");
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
        var selectedCodec = VideoCodec._012v; // dummy value
        foreach(var (key, value) in _globals.ValidVideoCodecsMap)
        {
            if (!key.Contains(settings.VideoCodec)) 
                continue;
            
            selectedCodec = value;
            break;
        }

        var videoCodecEnum = settings.VideoCodec is null
            ? VideoCodec.libx264
            : selectedCodec;

        var conversion = FFmpeg.Conversions.New()
            .SetPreset(ConversionPreset.VerySlow)
            .SetPixelFormat(PixelFormat.yuv420p)
            .AddParameter($"-crf {settings.Crf}");

        if (videoStream != null)
        {
            AddOptimizedFilter(conversion, videoStream, videoCodecEnum);
            conversion.AddStream(videoStream);
        }

        if (audioStream is null)
            return conversion;

        audioStream.SetBitrate(settings.AudioBitRate);
        audioStream.SetCodec(videoCodecEnum is VideoCodec.vp8 or VideoCodec.vp9 or VideoCodec.av1
            ? AudioCodec.libopus
            : AudioCodec.aac);
        conversion.AddStream(audioStream);

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
        const string twoCores = "-cpu-used 2";
        const string fourCore = "-cpu-used 4";
        const string sixCore = "-cpu-used 6";

        var cpuUsedParameter = framerate switch
        {
            < 24 => twoCores,
            > 60 => sixCore,
            _ => fourCore
        };

        conversion.AddParameter(cpuUsedParameter);
    }
}