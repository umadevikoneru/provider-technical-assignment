namespace Candidate.API.Data;

/// <summary>Who is making the change, recorded in UpdatedBy / DeletedBy and the audit table.</summary>
public interface ICurrentUser
{
    string Name { get; }
}

/// <summary>
/// The assignment has no authentication, so the caller may identify itself with an
/// X-User-Name header. With real auth this would read the authenticated principal instead.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public const string HeaderName = "X-User-Name";

    private const int MaxLength = 100;

    public string Name
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context is null)
            {
                return "system";
            }

            var header = context.Request.Headers[HeaderName].ToString().Trim();
            if (header.Length == 0)
            {
                return "api-user";
            }

            return header.Length > MaxLength ? header[..MaxLength] : header;
        }
    }
}
