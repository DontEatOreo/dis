using YoutubeDLSharp.Options;

namespace dis.CommandLineApp.Models;

public sealed record DownloadQuery(Uri Uri, OptionSet OptionSet);
