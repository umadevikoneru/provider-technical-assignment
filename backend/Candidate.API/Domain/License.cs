namespace Candidate.API.Domain;

public class License
{
    public int LicenseId { get; private set; }

    public int ProviderId { get; set; }

    public Provider Provider { get; set; } = null!;

    public string LicenseNumber { get; set; } = string.Empty;

    /// <summary>Status as recorded by staff. Use <see cref="LicenseView.EffectiveStatus"/> to decide validity.</summary>
    public LicenseStatus LicenseStatus { get; set; }

    public DateOnly ExpirationDate { get; set; }

    public DateTime CreatedDate { get; internal set; }

    public DateTime? UpdatedDate { get; internal set; }
}
