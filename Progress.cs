using Xabe.FFmpeg;
using YoutubeDLSharp;

namespace dis;

public class Progress
{
    public readonly Progress<DownloadProgress> YtDlProgress = new(p =>
    {
        if (p.Progress is 0)
            return;

        // Write the new progress message
        var downloadString = p.DownloadSpeed is not null
            ? $"Download Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}"
            : $"Download Progress: {p.Progress:P2}";
        Console.Write(downloadString);
    });

    public void ProgressBar(IConversion conversion)
    {
        conversion.OnProgress += (_, args) =>
        {
            var percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100);
            if (percent is 0)
                return;

            // Write the new progress message
            var progressMessage = $"Progress: {args.Duration.TotalSeconds / args.TotalLength.TotalSeconds:P2}";
            Console.Write($"\r{progressMessage}");
        };
    }
}