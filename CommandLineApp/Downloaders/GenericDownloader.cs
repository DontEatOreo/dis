using dis.CommandLineApp.Models;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public class GenericDownloader : VideoDownloaderBase
{
    public GenericDownloader(YoutubeDL youtubeDl, DownloadQuery downloadQuery)
        : base(youtubeDl, downloadQuery) { }
}
