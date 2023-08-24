using dis.CommandLineApp.Downloaders;
using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Interfaces;

public interface IVideoDownloader
{
    Task<DownloadResult> Download();
}
