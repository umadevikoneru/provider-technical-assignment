using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Services;

/// <summary>
/// Turns expected failures (business exceptions and database rule violations) into RFC 7807
/// ProblemDetails responses, so the UI gets one consistent error shape.
/// <para>
/// This is an MVC filter rather than an <c>IExceptionHandler</c> on purpose: it runs before the
/// exception-handling middleware, so an expected 404/409 is logged as information, not as an
/// error with a stack trace. Anything not mapped here is unexpected and falls through to
/// <c>UseExceptionHandler</c>, which logs it as an error and returns a 500.
/// </para>
/// </summary>
public sealed class ApiExceptionFilter(ProblemDetailsFactory problemDetailsFactory, ILogger<ApiExceptionFilter> logger)
    : IExceptionFilter
{
    private const int SqliteConstraint = 19;

    public void OnException(ExceptionContext context)
    {
        var (status, title, detail) = context.Exception switch
        {
            ApiException e => (e.StatusCode, e.Title, e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict",
                "This record was changed by someone else. Reload it and try again."),
            // A database rule (unique index, CHECK, or a trigger such as the hard-delete guard) rejected the change.
            DbUpdateException { InnerException: SqliteException { SqliteErrorCode: SqliteConstraint } sqlite } =>
                (StatusCodes.Status409Conflict, "Conflict", sqlite.Message),
            _ => (0, string.Empty, string.Empty)
        };

        if (status == 0)
        {
            return;
        }

        logger.LogInformation("Request failed with {Status}: {Detail}", status, detail);

        var problem = problemDetailsFactory.CreateProblemDetails(context.HttpContext, status, title, detail: detail);
        context.Result = new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }
}
