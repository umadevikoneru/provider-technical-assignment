namespace Candidate.API.Services;

/// <summary>
/// Base for expected business failures. Each subtype carries its own HTTP status, so adding
/// a new failure type needs no change to <see cref="ApiExceptionFilter"/> (open/closed).
/// </summary>
public abstract class ApiException(string message, int statusCode, string title) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public string Title { get; } = title;
}

/// <summary>The requested record does not exist, or is soft-deleted and so hidden from standard operations (404).</summary>
public sealed class NotFoundException(string message)
    : ApiException(message, StatusCodes.Status404NotFound, "Not found");

/// <summary>The request conflicts with current data, e.g. a duplicate or a stale version (409).</summary>
public sealed class ConflictException(string message)
    : ApiException(message, StatusCodes.Status409Conflict, "Conflict");
