using dis.CommandLineApp.Models;
using Serilog;
using Xabe.FFmpeg;

namespace dis.CommandLineApp.Conversion;

public sealed class Converter
{
    private readonly PathHandler _pathHandler;
    private readonly ProcessHandler _processHandler;
    private readonly ILogger _logger;

    public Converter(PathHandler pathHandler, ProcessHandler processHandler, ILogger logger)
    {
        _pathHandler = pathHandler;
        _processHandler = processHandler;
        _logger = logger;
    }

    public async Task ConvertVideo(string file, DateTime? dateTime, ParsedOptions options)
    {
        Console.CancelKeyPress += HandleCancellation;

        var compressPath = _pathHandler.GetCompressPath(file, options);
        var outputPath = _pathHandler.ConstructFilePath(options, compressPath);

        var mediaInfo = await FFmpeg.GetMediaInfo(file);
        var streams = mediaInfo.Streams;

        var conversion = _processHandler.ConfigureConversion(options, streams, outputPath);
        if (conversion is null)
        {
            _logger.Error("Could not configure conversion");
            return;
        }

        try
        {
            await conversion.Start();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Conversion failed");
            return;
        }

        var fileSize = new FileInfo(outputPath).Length;
        var fileSizeStr = fileSize < 1024 * 1024
            ? $"{fileSize / 1024.0:F2} KiB"
            : $"{fileSize / 1024.0 / 1024.0:F2} MiB";

        Console.WriteLine(); // New line after progress bar
        _logger.Information("Converted video saved at: {OutputFilePath} | Size: {FileSize}",
            outputPath,
            fileSizeStr);

        if (dateTime.HasValue)
            _processHandler.SetTimeStamps(outputPath, dateTime.Value);
    }

    private void HandleCancellation(object? sender, ConsoleCancelEventArgs e)
    {
        if (e.SpecialKey is not ConsoleSpecialKey.ControlC)
            return;
        _logger.Information("Canceled");
    }
}