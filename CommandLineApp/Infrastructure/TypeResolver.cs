using Spectre.Console.Cli;

namespace dis.CommandLineApp.Infrastructure;

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public object? Resolve(Type? type) => type == null ? null : _provider.GetService(type);

    public void Dispose() => (_provider as IDisposable)?.Dispose();
}
