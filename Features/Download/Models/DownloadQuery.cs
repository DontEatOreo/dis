using YoutubeDLSharp.Options;

namespace dis.Features.Download.Models;

public record DownloadQuery(Uri Uri, OptionSet OptionSet);