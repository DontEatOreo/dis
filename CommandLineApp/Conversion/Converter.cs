using System.Globalization;
using dis.CommandLineApp.Models;
using Serilog;
using Spectre.Console;
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
    public async Task ConvertVideo(string file, DateTime? dateTime, Settings o)
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
            await AnsiConsole.Status().StartAsync("Starting conversion...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Arrow);

                conversion.OnProgress += (_, args) =>
                {
                    var percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100);
                    if (percent is 0)
                        return;

                    ctx.Status($"[green]Conversion progress: {percent}%[/]");
                    ctx.Refresh();
                };
                await conversion.Start();
            });
            if (dateTime.HasValue)
                processHandler.SetTimeStamps(outP, dateTime.Value);
        }
        catch (Exception)
        {
            logger.Error("Conversion failed");
            logger.Error("FFmpeg args: {Conversion}", $"ffmpeg {conversion.Build().Trim()}");
            return;
        }

        AnsiConsole.MarkupLine($"Converted video saved at: [green]{outP}[/]");

        // Show how much space we saved
        var originalSize = new FileInfo(file).Length;
        var compressedSize = new FileInfo(outP).Length;

        var saved = originalSize - compressedSize;
        var savedPercent = (double)saved / originalSize * 100;
        var savedPercentRounded = Math.Round(savedPercent, 2)
            .ToString(CultureInfo.InvariantCulture);

        var originalMiB = originalSize / 1024.0 / 1024.0;
        var originalMiBRounded = Math.Round(originalMiB, 2)
            .ToString(CultureInfo.InvariantCulture);

        var compressedMiB = compressedSize / 1024.0 / 1024.0;
        var compressedMiBRounded = Math.Round(compressedMiB, 2)
            .ToString(CultureInfo.InvariantCulture);

        var originalSizeString = $"Original size: [red]{originalMiBRounded} MiB[/]";
        var compressedSizeString = compressedMiB > originalMiB
            ? $"Compressed size: [red]{compressedMiBRounded} MiB[/]"
            : $"Compressed size: [green]{compressedMiBRounded} MiB[/]";

        var savedString = saved < 0
            ? $"Increased: [red]{-saved / 1024.0 / 1024.0:F2} MiB (+{savedPercentRounded}%)[/]"
            : $"Saved: [green]{saved / 1024.0 / 1024.0:F2} MiB (-{savedPercentRounded}%)[/]";

        AnsiConsole.Write(new Grid()
            .AddColumns(new GridColumn(), new GridColumn(), new GridColumn())
            .AddRow(originalSizeString, compressedSizeString, savedString));
    }

    private void HandleCancellation(object? sender, ConsoleCancelEventArgs e)
    {
        if (e.SpecialKey is not ConsoleSpecialKey.ControlC)
            return;
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("Canceled");
    }
}
