using Xabe.FFmpeg;
using YoutubeDLSharp;

namespace dis;

public class Progress
{
    public static readonly Progress<DownloadProgress> YtDlProgress = new(p =>
    {
        if (p.Progress is 0)
            return;
        Console.Write(p.DownloadSpeed != null
            ? $"\rDownload Progress: {p.Progress:P2} | Download speed: {p.DownloadSpeed}\t"
            : $"\rDownload Progress: {p.Progress:P2}\t");
    });

    public static void FFmpegProgressBar(IConversion conversion)
    {
        conversion.OnProgress += (_, args) =>
        {
            var percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100);
            if (percent is 0)
                return;
            Console.Write($"\rProgress: {percent}%");
        };
    }
}