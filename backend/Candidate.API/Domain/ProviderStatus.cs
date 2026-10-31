namespace Candidate.API.Domain;

/// <summary>Business lifecycle state of a provider. Stored as text (see 01_schema.sql CHECK constraint).</summary>
public enum ProviderStatus
{
    Active,
    Inactive,
    Pending
}
