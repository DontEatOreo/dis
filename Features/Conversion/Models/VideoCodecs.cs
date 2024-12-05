using Xabe.FFmpeg;

namespace dis.Features.Conversion.Models;

public sealed class VideoCodecs
{
    public readonly Dictionary<string[], VideoCodec> Codecs = new()
    {
        { ["h264", "libx264"], VideoCodec.libx264 },
        { ["h265", "libx265", "hevc"], VideoCodec.hevc },
        { ["vp8", "libvpx"], VideoCodec.vp8 },
        { ["vp9", "libvpx-vp9"], VideoCodec.vp9 },
        { ["av1", "libaom-av1"], VideoCodec.av1 }
    };
}
