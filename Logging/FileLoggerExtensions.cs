using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LanzaTuIdea.Api.Logging;

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFileLogging(this ILoggingBuilder builder, IConfiguration configuration)
    {
        var options = new FileLoggerOptions();
        configuration.GetSection("FileLogging").Bind(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
        return builder;
    }
}
