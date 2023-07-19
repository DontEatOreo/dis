using System.CommandLine.Parsing;

namespace dis.CommandLineApp.Interfaces;

public interface ICommandLineValidator
{
    void ValidateInputs(OptionResult result);
    void ValidateOutput(OptionResult result);
    void ValidateCrf(OptionResult result);
    void ValidateAudioBitrate(OptionResult result);
    void ValidateVideoCodec(OptionResult result);
    void ValidateResolution(OptionResult result);
}