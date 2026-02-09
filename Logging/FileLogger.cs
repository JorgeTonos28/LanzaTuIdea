using System.Text;
using Microsoft.Extensions.Logging;

namespace LanzaTuIdea.Api.Logging;

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerOptions _options;
    private readonly Func<IExternalScopeProvider?> _scopeProviderAccessor;
    private readonly object _writeLock = new();

    public FileLogger(string categoryName, FileLoggerOptions options, Func<IExternalScopeProvider?> scopeProviderAccessor)
    {
        _categoryName = categoryName;
        _options = options;
        _scopeProviderAccessor = scopeProviderAccessor;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _scopeProviderAccessor()?.Push(state) ?? default;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _options.MinimumLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message) && exception is null)
        {
            return;
        }

        var timestamp = _options.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        var builder = new StringBuilder();
        builder.Append(timestamp.ToString("O"));
        builder.Append(" [").Append(logLevel).Append("] ");
        builder.Append(_categoryName).Append(": ").Append(message);

        if (exception is not null)
        {
            builder.AppendLine();
            builder.Append(exception);
        }

        if (_options.IncludeScopes)
        {
            var scopeProvider = _scopeProviderAccessor();
            scopeProvider?.ForEachScope((scope, sb) =>
            {
                sb.Append(" => ").Append(scope);
            }, builder);
        }

        var logLine = builder.ToString();
        var directory = Path.IsPathRooted(_options.LogDirectory)
            ? _options.LogDirectory
            : Path.Combine(AppContext.BaseDirectory, _options.LogDirectory);

        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"{_options.FileNamePrefix}-{timestamp:yyyyMMdd}.log");

        lock (_writeLock)
        {
            File.AppendAllText(filePath, logLine + Environment.NewLine);
        }
    }
}
