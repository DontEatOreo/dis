using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Conversion;

public sealed class PathHandler
{
    private readonly CodecParser _codecParser;
    private readonly Globals _globals;

    public PathHandler(Globals globals, CodecParser codecParser)
    {
        _globals = globals;
        _codecParser = codecParser;
    }

    public string GetCompressPath(string file, ParsedOptions options)
    {
        var videoExtMap = _globals.VideoExtMap;
        _ = _codecParser.TryParseCodec(options.VideoCodec, out var videoCodec);

        string? extension = null;
        foreach (var kvp in
                 videoExtMap.Where(kvp => kvp.Value.Contains(videoCodec)))
        {
            extension = kvp.Key;
            return Path.ChangeExtension(file, extension);
        }

        return Path.ChangeExtension(file, extension);
    }

    public string ConstructFilePath(ParsedOptions options, string compressedVideoPath)
    {
        var uuid = Guid.NewGuid().ToString()[..4];

        var ogFileName = Path.GetFileName(compressedVideoPath);
        if (ogFileName is null)
            throw new Exception("Could not get the original file name");

        var outputFilePath = Path.Combine(options.Output, ogFileName);
        var ogExtension = Path.GetExtension(compressedVideoPath);

        var outputFileName = File.Exists(outputFilePath)
            ? $"{Path.GetFileNameWithoutExtension(ogFileName)}-{uuid}{ogExtension}"
            : ogFileName;

        if (options.RandomFileName)
            outputFileName = $"{uuid}{ogExtension}";

        return Path.Combine(options.Output, outputFileName);
    }
}