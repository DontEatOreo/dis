using System.CommandLine;
using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Interfaces;

public interface ICommandLineApp
{
    Task Handler(ParsedOptions o);

    ParsedOptions ParseOptions(ParseResult result, UnParsedOptions unparsed);
}