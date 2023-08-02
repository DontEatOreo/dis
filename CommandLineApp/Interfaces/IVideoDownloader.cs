namespace dis.CommandLineApp.Interfaces;

public interface IVideoDownloader
{
    Task<(string?, DateTime?)> Download();
}
