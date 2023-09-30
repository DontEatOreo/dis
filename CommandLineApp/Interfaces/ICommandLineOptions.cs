using System.CommandLine;
using dis.CommandLineApp.Models;

namespace dis.CommandLineApp.Interfaces;

public interface ICommandLineOptions
{
    (CliConfiguration, UnParsedOptions) GetCommandLineOptions();
}