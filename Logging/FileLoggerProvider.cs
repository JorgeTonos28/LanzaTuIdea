using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LanzaTuIdea.Api.Logging;

public class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly FileLoggerOptions _options;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private IExternalScopeProvider? _scopeProvider;

    public FileLoggerProvider(FileLoggerOptions options)
    {
        _options = options;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _options, () => _scopeProvider));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }
}
