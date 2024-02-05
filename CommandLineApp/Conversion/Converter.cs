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

        /*
         * File sizes are read and compared to calculate the difference and percentage saved.
         *
         * Sizes are shown in bytes, KiB, or MiB, depending on the magnitude. If file sizes are less than 1 MiB,
         * they are represented in KiB for better readability. Savings (and increases, if present) are color-coded
         * for user convenience.
         *
         * The results are presented in a table with columns "Original", "Compressed", and "Saved".
         *
         * For example:
         * ┌─────────────────────────┬─────────────────────────────┬────────────────────────────┐
         * │ Original                │ Compressed                  │ Saved                      │
         * ├─────────────────────────┼─────────────────────────────┼────────────────────────────┤
         * │ Original size: 1.10 MiB │ Compressed size: 417.28 KiB │ Saved: 709.82 KiB (62.98%) │
         * └─────────────────────────┴─────────────────────────────┴────────────────────────────┘
         */

        var originalSize = (double)new FileInfo(file).Length;
        var compressedSize = (double)new FileInfo(outP).Length;

        var saved = originalSize - compressedSize;
        var savedPercent = saved / originalSize * 100;
        var savedPercentRounded = Math.Round(savedPercent, 2)
            .ToString(CultureInfo.InvariantCulture);

        var originalMiB = originalSize / 1024.0 / 1024.0;
        var originalKiB = originalSize / 1024.0;
        var originalSizeString = originalMiB < 1
            ? $"Original size: [red]{Math.Round(originalKiB, 2):F2} KiB[/]"
            : $"Original size: [red]{Math.Round(originalMiB, 2):F2} MiB[/]";

        var compressedMiB = compressedSize / 1024.0 / 1024.0;
        var compressedKiB = compressedSize / 1024.0;
        var compressedColor = compressedSize > originalSize ? "red" : "green";
        var compressedSizeString = compressedMiB < 1
            ? $"Compressed size: [{compressedColor}]{Math.Round(compressedKiB, 2):F2} KiB[/]"
            : $"Compressed size: [{compressedColor}]{Math.Round(compressedMiB, 2):F2} MiB[/]";

        var savedMiB = Math.Abs(saved) / 1024.0 / 1024.0;
        var savedKiB = Math.Abs(saved) / 1024.0;
        var savedColor = saved < 0 ? "red" : "green";
        var savedChange = saved < 0 ? "Increased" : "Saved";
        var savedSymbol = saved < 0 ? "+" : "-";
        var savedSizeString = savedMiB < 1
            ? $"{savedSymbol}{Math.Round(savedKiB, 2):F2} KiB"
            : $"{savedSymbol}{Math.Round(savedMiB, 2):F2} MiB";
        var savedString = $"{savedChange}: [{savedColor}]{savedSizeString} ({savedPercentRounded}%)[/]";


        var table = new Table();
        table.AddColumn("Original");
        table.AddColumn("Compressed");
        table.AddColumn(savedChange);
        table.AddRow(originalSizeString, compressedSizeString, savedString);

        AnsiConsole.Write(table);
    }

    private void HandleCancellation(object? sender, ConsoleCancelEventArgs e)
    {
        if (e.SpecialKey is not ConsoleSpecialKey.ControlC)
            return;
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("Canceled");
    }
}
