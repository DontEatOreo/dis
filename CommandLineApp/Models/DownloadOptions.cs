namespace dis.CommandLineApp.Models;

public sealed record DownloadOptions(Uri Uri, string? Trim, bool KeepWatermark, bool SponsorBlock);
