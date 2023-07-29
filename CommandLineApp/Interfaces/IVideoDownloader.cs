namespace dis.CommandLineApp.Interfaces;

public interface IVideoDownloader
{
    Task<string?> Download();
}
