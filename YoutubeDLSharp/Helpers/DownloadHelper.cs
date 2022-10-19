using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace dis.YoutubeDLSharp.Helpers;

internal static class DownloadHelper
{
    /// <summary>
    /// Downloads the YT-DLP binary depending on OS
    /// </summary>
    /// <param name="directoryPath">The optional directory of where it should be saved to</param>
    /// <exception cref="Exception"></exception>
    internal static async Task DownloadYtDlp(string? directoryPath = default)
    {
        const string? baseGithubUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp";
        
        var downloadUrl = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) switch
        {
            true => $"{baseGithubUrl}.exe",
            false => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) switch
            {
                true => $"{baseGithubUrl}_macos",
                false => baseGithubUrl
            }
        };

        if (string.IsNullOrEmpty(directoryPath)) { directoryPath = Directory.GetCurrentDirectory(); }

        var downloadLocation = Path.Combine(directoryPath, "yt-dlp");
        var data = Task.Run(() => DownloadFileBytesAsync(downloadUrl)).Result;
        await File.WriteAllBytesAsync(downloadLocation, data);
    }

    /// <summary>
    /// Downloads the FFmpeg binary depending on the OS
    /// </summary>
    /// <param name="directoryPath">The optional directory of where it should be saved to</param>
    /// <exception cref="Exception"></exception>
    internal static async Task DownloadFFmpeg(string directoryPath = "null")
    {
        if (string.IsNullOrEmpty(directoryPath)) { directoryPath = Directory.GetCurrentDirectory(); }
        const string ffmpegApiUrl = "https://ffbinaries.com/api/v1/version/latest";
        HttpClient httpClient = new();
        
        var ffmpegVersion = JsonConvert.DeserializeObject<FFmpegApi.Root>(await (await httpClient.GetAsync(ffmpegApiUrl)).Content.ReadAsStringAsync());
        
        var downloadUrl = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) switch
        {
            true => ffmpegVersion?.Bin.Windows64.Ffmpeg,
            false => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) switch
            {
                true => ffmpegVersion?.Bin.Osx64.Ffmpeg,
                false => ffmpegVersion?.Bin.Linux64.Ffmpeg
            }
        };
        
        var downloadLocation = Path.Combine(directoryPath, Path.GetFileName(downloadUrl) ?? string.Empty);
        var data = await DownloadFileBytesAsync(downloadUrl);
        await File.WriteAllBytesAsync(downloadLocation, data);
    }

    /// <summary>
    /// Downloads a file from the specified URI
    /// </summary>
    /// <param name="uri">The URI of the file to download</param>
    /// <returns>Returns a byte array of the file that was downloaded</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static async Task<byte[]> DownloadFileBytesAsync(string? uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
            throw new InvalidOperationException("URI is invalid.");

        var httpClient = new HttpClient();
        var fileBytes = await httpClient.GetByteArrayAsync(uri);
        return fileBytes;
    }
}

internal class FFmpegApi
{
    public class Bin
    {
        public Bin(Windows64 windows64, Linux64 linux64, LinuxArm64 linuxArm64, Osx64 osx64)
        {
            Windows64 = windows64;
            Linux64 = linux64;
            LinuxArm64 = linuxArm64;
            Osx64 = osx64;
        }

        [JsonProperty("windows-64")]
        public Windows64 Windows64 { get; set; }

        [JsonProperty("linux-64")]
        public Linux64 Linux64 { get; set; }

        [JsonProperty("linux-arm64")]
        public LinuxArm64 LinuxArm64 { get; set; }

        [JsonProperty("osx-64")]
        public Osx64 Osx64 { get; set; }
    }

    public class Linux64
    {
        public Linux64(string ffmpeg)
        {
            Ffmpeg = ffmpeg;
        }

        [JsonProperty("ffmpeg")]
        public string Ffmpeg { get; set; }
    }

    public class LinuxArm64
    {
        public LinuxArm64(string ffmpeg)
        {
            Ffmpeg = ffmpeg;
        }

        [JsonProperty("ffmpeg")]
        public string Ffmpeg { get; set; }
    }

    public class Osx64
    {
        public Osx64(string ffmpeg)
        {
            Ffmpeg = ffmpeg;
        }

        [JsonProperty("ffmpeg")]
        public string Ffmpeg { get; set; }
    }

    public class Root
    {
        public Root(Bin bin)
        {
            Bin = bin;
        }

        [JsonProperty("bin")]
        public Bin Bin { get; set; }
    }

    public class Windows64
    {
        public Windows64(string ffmpeg)
        {
            Ffmpeg = ffmpeg;
        }

        [JsonProperty("ffmpeg")]
        public string Ffmpeg { get; set; }
    }
}