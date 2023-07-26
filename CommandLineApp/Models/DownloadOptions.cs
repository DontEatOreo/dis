namespace dis.CommandLineApp.Models;

public sealed record DownloadOptions(Uri Uri, bool KeepWatermark, bool SponsorBlock);
