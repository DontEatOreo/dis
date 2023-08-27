using Xabe.FFmpeg;

namespace dis.CommandLineApp.Conversion;

public sealed class CodecParser
{
    private readonly Globals _globals;

    public CodecParser(Globals globals)
    {
        _globals = globals;
    }

    public bool TryParseCodec(string? inputCodec, out VideoCodec outputCodec)
    {
        if (inputCodec is null)
        {
            outputCodec = VideoCodec.libx264;
            return false;
        }

        var validCodecs = _globals.ValidVideoCodecsMap;
        foreach (var (key, value) in validCodecs)
        {
            if (key.Contains(inputCodec) is false)
                continue;

            outputCodec = value;
            return true;
        }

        outputCodec = VideoCodec.libx264;
        return false;
    }
}