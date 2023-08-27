using dis.CommandLineApp.Models;
using Serilog;
using Xabe.FFmpeg;

namespace dis;

public sealed class Converter
{
    private readonly Globals _globals;
    private readonly ILogger _logger;

    public Converter(Globals globals, ILogger logger)
    {
        _globals = globals;
        _logger = logger;
    }

    public async Task ConvertVideo(string path, DateTime? time, ParsedOptions options)
    {
        var selectedCodec = VideoCodec._012v; // dummy value
        foreach (var (key, value) in _globals.ValidVideoCodecsMap)
        {
            if (!key.Contains(options.VideoCodec))
                continue;

            selectedCodec = value;
            break;
        }

        var videoCodecEnum = options.VideoCodec is null
            ? VideoCodec.libx264
            : selectedCodec;

        if (_globals.ResolutionList.Contains(options.Resolution) is false)
        {
            _logger.Error("Invalid resolution");
            return;
        }

        var compressedVideoPath = GetCompressedVideoPath(path, videoCodecEnum);
        var outputFilePath = ConstructFilePath(options, compressedVideoPath);

        HandleCancellation(outputFilePath);

        var mediaInfo = await FFmpeg.GetMediaInfo(path);

        var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
        var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

        if (videoStream is null)
        {
            _logger.Error("There is no video or audio stream in the file");
            return;
        }

        if (options.Resolution is not null)
            SetRes(videoStream, options.Resolution);

        var conversion = ConfigureConversion(options, videoStream, audioStream);

        conversion.OnProgress += (_, args) =>
        {
            var percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100);
            if (percent is 0)
                return;

            // Write the new progress message
            var progressMessage = $"\rProgress: {args.Duration.TotalSeconds / args.TotalLength.TotalSeconds:P2}";
            Console.Write(progressMessage);
        };

        conversion.SetOutput(outputFilePath);
        await conversion.Start();

        if (time.HasValue)
        {
            File.SetCreationTime(outputFilePath, time.Value);
            File.SetLastWriteTime(outputFilePath, time.Value);
            File.SetLastAccessTime(outputFilePath, time.Value);
        }

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

    private static string ConstructFilePath(ParsedOptions options, string compressedVideoPath)
    {
        var uuid = Guid.NewGuid().ToString()[..4];

        var ogFileName = Path.GetFileName(compressedVideoPath);
        if (ogFileName is null)
            throw new Exception("Could not get the original file name");

        var outputFilePath = Path.Combine(options.Output, ogFileName);
        var ogExtension = Path.GetExtension(compressedVideoPath);

        var outputFileName = File.Exists(outputFilePath)
            ? $"{Path.GetFileNameWithoutExtension(ogFileName)}-{uuid}{ogExtension}"
            : ogFileName;

        if (options.RandomFileName)
            outputFileName = $"{uuid}{ogExtension}";

        return Path.Combine(options.Output, outputFileName);
    }

    private void HandleCancellation(string outputFilePath)
    {
        Console.CancelKeyPress += (_, args) =>
        {
            if (args.SpecialKey is not ConsoleSpecialKey.ControlC)
                return;
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

    private IConversion ConfigureConversion(ParsedOptions options, IVideoStream? videoStream, IAudioStream? audioStream)
    {
        var selectedCodec = VideoCodec._012v; // dummy value
        foreach (var (key, value) in _globals.ValidVideoCodecsMap)
        {
            if (!key.Contains(options.VideoCodec))
                continue;

            selectedCodec = value;
            break;
        }

        var videoCodecEnum = options.VideoCodec is null
            ? VideoCodec.libx264
            : selectedCodec;

        var conversion = FFmpeg.Conversions.New()
            .SetPreset(ConversionPreset.VerySlow)
            .SetPixelFormat(PixelFormat.yuv420p)
            .AddParameter($"-crf {options.Crf}");

        if (videoStream != null)
        {
            AddOptimizedFilter(conversion, videoStream, videoCodecEnum);
            conversion.AddStream(videoStream);
        }

        if (audioStream is null)
            return conversion;

        audioStream.SetBitrate(options.AudioBitrate);
        audioStream.SetCodec(videoCodecEnum
            is VideoCodec.vp8 or VideoCodec.vp9 or VideoCodec.av1
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
