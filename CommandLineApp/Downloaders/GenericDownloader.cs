using dis.CommandLineApp.Models;
using YoutubeDLSharp;

namespace dis.CommandLineApp.Downloaders;

public sealed class GenericDownloader(YoutubeDL youtubeDl, DownloadQuery query) : VideoDownloaderBase(youtubeDl, query);
