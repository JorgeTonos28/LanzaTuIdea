using Microsoft.Extensions.Logging;

namespace LanzaTuIdea.Api.Logging;

public class FileLoggerOptions
{
    public string LogDirectory { get; set; } = "logs";
    public string FileNamePrefix { get; set; } = "lanza-tu-idea";
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    public bool IncludeScopes { get; set; } = false;
    public bool UseUtcTimestamp { get; set; } = false;
}
