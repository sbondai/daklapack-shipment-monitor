using DaklaPack.Shipments.Domain.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DaklaPack.Shipments.Api.Configuration;

/// <summary>
/// Turns unhandled exceptions into RFC 7807 problem responses, so no controller needs a try/catch.
/// </summary>
/// <remarks>
/// The client never receives a stack trace or an internal message: those go to the log, and the
/// response carries only what is safe to show. Validation failures never reach here —
/// <c>[ApiController]</c> handles those before the action runs.
/// </remarks>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // A DomainException means stored data violated an invariant: still a 500, but logged
        // distinctly because the cause and the fix differ from an arbitrary fault.
        if (exception is DomainException)
        {
            LogDomainInvariantViolated(logger, exception, httpContext.Request.Path);
        }
        else
        {
            LogUnhandled(logger, exception, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1"
            }
        });
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "Domain invariant violated while serving {Path}")]
    private static partial void LogDomainInvariantViolated(ILogger logger, Exception exception, string path);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Unhandled exception while serving {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string path);
}
