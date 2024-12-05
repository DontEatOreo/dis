using dis.Features.Common;

namespace dis.Features.Download.Models;

public record DownloadOptions(Uri Uri, Settings Options, TrimSettings? TrimSettings = null);
