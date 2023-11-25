using dis.CommandLineApp.Models;
using Serilog;
using Xabe.FFmpeg;

namespace dis.CommandLineApp.Conversion;

public sealed class Converter(PathHandler pathHandler, ProcessHandler processHandler, ILogger logger)
{
    /// <summary>
    /// Converts a video file to a specified format using FFmpeg.
    /// </summary>
    /// <param name="file">The path to the input video file.</param>
    /// <param name="dateTime">The optional date and time to set for the output file.</param>
    /// <param name="o">The options to use for the conversion.</param>
    /// <returns>A task that represents the asynchronous conversion operation.</returns>
    public async Task ConvertVideo(string file, DateTime? dateTime, ParsedOptions o)
    {
        Console.CancelKeyPress += HandleCancellation;

        var cmpPath = pathHandler.GetCompressPath(file, o.VideoCodec);
        var outP = pathHandler.ConstructFilePath(o, cmpPath);

        var mediaInfo = await FFmpeg.GetMediaInfo(file);
        var streams = mediaInfo.Streams;

        var conversion = processHandler.ConfigureConversion(o, streams, outP);
        if (conversion is null)
        {
            logger.Error("Could not configure conversion");
            return;
        }

        try
        {
            await conversion.Start();
            if (dateTime.HasValue)
                processHandler.SetTimeStamps(outP, dateTime.Value);
        }
        catch (Exception)
        {
            logger.Error( "Conversion failed");
            logger.Error("FFmpeg args: {Conversion}", $"ffmpeg {conversion.Build().Trim()}");
            return;
        }

        var fileSize = new FileInfo(outP).Length;
        var fileSizeStr = fileSize < 1024 * 1024
            ? $"{fileSize / 1024.0:F2} KiB"
            : $"{fileSize / 1024.0 / 1024.0:F2} MiB";

        Console.WriteLine(); // New line after progress bar
        logger.Information("Converted video saved at: {OutputFilePath} | Size: {FileSize}",
            outP,
            fileSizeStr);
    }

    private void HandleCancellation(object? sender, ConsoleCancelEventArgs e)
    {
        if (e.SpecialKey is not ConsoleSpecialKey.ControlC)
            return;
        Console.WriteLine();
        logger.Information("Canceled");
    }
}