using Xabe.FFmpeg;
using YoutubeDLSharp;

namespace dis;

public class Progress
{
    public readonly Progress<DownloadProgress> YtDlProgress = new(p =>
    {
        if (p.Progress is 0)
            return;

        // Clear the previous progress message
        Console.Write($"\r{new string(' ', Console.WindowWidth - 1)}\r");

        // Write the new progress message
        Console.Write(p.DownloadSpeed != null
            ? $"Download Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}"
            : $"Download Progress: {p.Progress:P2}");
    });

    public void ProgressBar(IConversion conversion)
    {
        conversion.OnProgress += (_, args) =>
        {
            var percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100);
            if (percent is 0)
                return;

            // Clear the previous progress message
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

            // Write the new progress message
            Console.Write($"Progress: {args.Duration.TotalSeconds / args.TotalLength.TotalSeconds:P2}");
        };
    }
}