using Xabe.FFmpeg;

namespace dis.Features.Conversion;

public sealed class StreamConfigurator
{
    private readonly string[] _av1Args =
    [
        "-lag-in-frames 48",
        "-row-mt 1",
        "-tile-rows 0",
        "-tile-columns 1"
    ];

    private readonly string[] _vp9Args =
    [
        "-row-mt 1",
        "-lag-in-frames 25",
        "-cpu-used 4",
        "-auto-alt-ref 1",
        "-arnr-maxframes 7",
        "-arnr-strength 4",
        "-aq-mode 0",
        "-enable-tpl 1",
        "-row-mt 1"
    ];

    public void SetResolution(IVideoStream stream, string res)
    {
        double width = stream.Width;
        double height = stream.Height;

        var resInt = int.Parse(res[..^1]); // 1080p -> 1080
        var aspectRatio = width / height;

        var outputWidth = (int)Math.Round(resInt * aspectRatio);
        var outputHeight = resInt;

        outputWidth -= outputWidth % 2;
        outputHeight -= outputHeight % 2;

        stream.SetSize(outputWidth, outputHeight);
    }

    public void SetCpuForAv1(IConversion conversion, double framerate)
    {
        const string twoCores = "-cpu-used 2";
        const string fourCore = "-cpu-used 4";
        const string sixCore = "-cpu-used 6";

        var cpuUsedParameter = framerate switch
        {
            < 24 => twoCores,
            > 60 => sixCore,
            _ => fourCore
        };

        conversion.AddParameter(string.Join(" ", _av1Args));
        conversion.AddParameter(cpuUsedParameter);
    }

    public void SetVp9Args(IConversion conversion)
        => conversion.AddParameter(string.Join(" ", _vp9Args));
}
