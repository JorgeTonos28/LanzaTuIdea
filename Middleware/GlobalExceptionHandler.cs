using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LanzaTuIdea.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var errorId = Guid.NewGuid().ToString("N");
        var traceId = httpContext.TraceIdentifier;
        _logger.LogError(exception, "Unhandled exception. ErrorId: {ErrorId} TraceId: {TraceId}", errorId, traceId);

        var detail = _environment.IsDevelopment()
            ? exception.ToString()
            : $"Referencia de error: {errorId}";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocurrió un error interno.",
            Detail = detail
        };

        problemDetails.Extensions["errorId"] = errorId;
        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
