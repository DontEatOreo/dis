using System.CommandLine.Parsing;

namespace dis.CommandLineApp.Interfaces;

public interface ICommandLineValidator
{
    void Inputs(OptionResult result);
    void Output(OptionResult result);
    void Crf(OptionResult result);
    void AudioBitRate(OptionResult result);
    void VideoCodec(OptionResult result);
    void Resolution(OptionResult result);
    void Trim(OptionResult result);
    void MultiThread(OptionResult obj);
}
