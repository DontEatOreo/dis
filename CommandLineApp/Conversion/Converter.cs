using System.Globalization;
using dis.CommandLineApp.Models;
using Serilog;
using Spectre.Console;
using Xabe.FFmpeg;

namespace dis.CommandLineApp.Conversion;

public sealed class Converter(PathHandler pathHandler, Globals globals, ProcessHandler processHandler, ILogger logger)
{
    /// <summary>
    /// Converts a video file to a specified format using FFmpeg.
    /// </summary>
    /// <param name="file">The path to the input video file.</param>
    /// <param name="dateTime">The optional date and time to set for the output file.</param>
    /// <param name="s">The options to use for the conversion.</param>
    /// <returns>A task that represents the asynchronous conversion operation.</returns>
    public async Task ConvertVideo(string file, DateTime? dateTime, Settings s)
    {
        while (true)
        {
            Console.CancelKeyPress += HandleCancellation;

            var cmpPath = pathHandler.GetCompressPath(file, s.VideoCodec);
            var outP = pathHandler.ConstructFilePath(s, cmpPath);

            var mediaInfo = await FFmpeg.GetMediaInfo(file);
            var streams = mediaInfo.Streams.ToList();

            var conversion = processHandler.ConfigureConversion(s, streams, outP);
            if (conversion is null)
            {
                logger.Error("Could not configure conversion");
                return;
            }

            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Starting conversion...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Arrow);

                        conversion.OnProgress += (_, args) =>
                        {
                            var percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100);
                            if (percent is 0) return;

                            ctx.Status($"[green]Conversion progress: {percent}%[/]");
                            ctx.Refresh();
                        };
                        await conversion.Start();
                    });
                if (dateTime.HasValue) processHandler.SetTimeStamps(outP, dateTime.Value);
            }
            catch (Exception)
            {
                logger.Error("Conversion failed");
                logger.Error("FFmpeg args: {Conversion}", $"ffmpeg {conversion.Build().Trim()}");
                return;
            }

            AnsiConsole.MarkupLine($"Converted video saved at: [green]{outP}[/]");

            var (originalSize, compressedSize) = ResultsTable(file, outP);
            if (compressedSize > originalSize)
            {
                var videoStream = streams.OfType<IVideoStream>().FirstOrDefault();
                if (videoStream is not null)
                    if (ShouldRetry(s, outP, streams)) continue;
            }

            break;
        }
    }

    private static (double, double) ResultsTable(string file, string outP)
    {
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
         * ┌───────────────────────────┬─────────────────────────────┬─────────────────────────────┐
         * │ Original                  │ Compressed                  │ Saved                       │
         * ├───────────────────────────┼─────────────────────────────┼─────────────────────────────┤
         * │ Original size: 710.86 KiB │ Compressed size: 467.02 KiB │ Saved: -243.84 KiB (-34.3%) │
         * └───────────────────────────┴─────────────────────────────┴─────────────────────────────┘ 
         */
        var originalSize = (double)new FileInfo(file).Length;
        var compressedSize = (double)new FileInfo(outP).Length;

        var saved = originalSize - compressedSize;
        var savedPercent = saved / originalSize * 100;
        var savedPercentRounded = (savedPercent > 0 ? "-" : "+") + Math.Round(Math.Abs(savedPercent), 2)
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
        return (originalSize, compressedSize);
    }

    private bool ShouldRetry(Settings s, string outP, IEnumerable<IStream> streams)
    {
        AnsiConsole.MarkupLine("[yellow]The resulting file is larger than the original.[/]");

        var deleteAndRetry = AskForRetry();
        if (deleteAndRetry is false) return false;
        if (File.Exists(outP))
        {
            File.Delete(outP);
            AnsiConsole.MarkupLine("[green]Deleted the converted video.[/]");
        }

        var resolutionChanged = AskForResolutionChange(streams, s);
        var crfChanged = AskForCrfChange(s);

        return resolutionChanged || crfChanged;
    }

    private static bool AskForRetry()
    {
        var deleteAndRetry = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Do you want to delete the converted video and try again with a better setting?")
                .AddChoices(["Yes", "No"]));

        return deleteAndRetry is "Yes";
    }

    private bool AskForResolutionChange(IEnumerable<IStream> streams, Settings s)
    {
        var resolutionChanged = false;
        var changeResolution = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Would you like to change the resolution?")
                .AddChoices(["Yes", "No"]));

        if (changeResolution is "Yes") resolutionChanged = ChangeResolution(streams, s);
        return resolutionChanged;
    }

    private bool ChangeResolution(IEnumerable<IStream> streams, Settings s)
    {
        var videoStream = streams.OfType<IVideoStream>().First();
        var width = videoStream.Width;
        var height = videoStream.Height;

        var maxDimension = height > width ? height : Math.Max(width, height);
        var currentResolution = GetClosestResolution(maxDimension);

        var resolutionList = globals.ValidResolutions.ToList();
        var currentResolutionIndex = resolutionList.IndexOf(currentResolution);

        if (currentResolutionIndex <= 0) return false;

        var lowerResolutions = resolutionList.GetRange(0, currentResolutionIndex);
        var chosenResolution = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Please select a lower resolution for the conversion:")
                .AddChoices(lowerResolutions.Select(res => res + "p")));

        s.Resolution = chosenResolution;
        return true;
    }

    private int GetClosestResolution(int dimension)
    {
        var closestResolutionIndex = globals.ValidResolutions.BinarySearch(dimension);

        // If dimension is not found, BinarySearch returns a negative number that is the bitwise complement 
        // of the index of the next element that is larger than dimension or, if there is no larger element, 
        // the bitwise complement of Count. So we have to handle this case and revert it to the correct value.
        if (closestResolutionIndex < 0) closestResolutionIndex = ~closestResolutionIndex - 1;

        // if there's no resolution smaller than dimension, choose the smallest one
        if (closestResolutionIndex < 0) closestResolutionIndex = 0;

        return globals.ValidResolutions[closestResolutionIndex];
    }

    private static bool AskForCrfChange(Settings s)
    {
        var crfChanged = false;
        var crfAnswer = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("Would you like to enter a new crf value?")
            .AddChoices(["Yes", "No"]));

        if (crfAnswer is "Yes") crfChanged = ChangeCrfValue(s);

        return crfChanged;
    }

    private static bool ChangeCrfValue(Settings s)
    {
        bool crfChanged;

        while (true)
        {
            var crfAnswer = AnsiConsole.Ask<string>("Please enter new value:");
            var crfValue = int.Parse(new string(crfAnswer.Where(char.IsDigit).ToArray()));

            if (crfValue <= s.Crf)
            {
                AnsiConsole.WriteLine("Please enter a value higher than the current CRF.");
                continue;
            }

            s.Crf = crfValue;
            crfChanged = true;
            break;
        }

        return crfChanged;
    }

    private static void HandleCancellation(object? sender, ConsoleCancelEventArgs e)
    {
        if (e.SpecialKey is not ConsoleSpecialKey.ControlC)
            return;

        AnsiConsole.WriteLine("Canceled");
    }
}
