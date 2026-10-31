using System.ComponentModel.DataAnnotations;
using Candidate.API.Domain;

namespace Candidate.API.Contracts;

/// <summary>Fields a client may set on a provider. Audit and soft-delete columns are server-owned.</summary>
public class ProviderWriteRequest
{
    [Required(ErrorMessage = "Provider name is required.")]
    [StringLength(200, ErrorMessage = "Provider name must be 200 characters or fewer.")]
    public string? ProviderName { get; init; }

    [Required(ErrorMessage = "County is required.")]
    [StringLength(100, ErrorMessage = "County must be 100 characters or fewer.")]
    public string? County { get; init; }

    [Required(ErrorMessage = "Status is required.")]
    [EnumName(typeof(ProviderStatus))]
    public string? Status { get; init; }

    /// <summary>Only call after model validation has passed.</summary>
    public ProviderStatus ParseStatus() => Enum.Parse<ProviderStatus>(Status!.Trim(), ignoreCase: true);
}

public sealed class ProviderCreateRequest : ProviderWriteRequest;

public sealed class ProviderUpdateRequest : ProviderWriteRequest
{
    /// <summary>The Version the client last read. A mismatch means someone else changed the provider (409).</summary>
    [Required(ErrorMessage = "Version is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Version must be a positive number.")]
    public int? Version { get; init; }
}
