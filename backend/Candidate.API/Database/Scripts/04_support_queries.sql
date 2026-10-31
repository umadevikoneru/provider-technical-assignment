-- =============================================================================
-- 04_support_queries.sql
-- Ad-hoc queries for support staff. Read-only; not run by the application.
-- =============================================================================

-- Scenario 1: active providers and their active licenses
SELECT * FROM vw_ActiveProvidersActiveLicenses ORDER BY ProviderName, LicenseNumber;

-- Scenario 2: providers that appear active but have expired licenses
SELECT * FROM vw_ActiveProvidersWithExpiredLicenses ORDER BY DaysSinceExpiration DESC;

-- Scenario 3: standard provider list (soft-deleted providers excluded)
SELECT * FROM vw_ActiveProviders ORDER BY ProviderName;

-- Audit: soft-deleted providers, still present in the table
SELECT * FROM vw_DeletedProviders ORDER BY DeletedDate DESC;

-- Audit: full change history for one provider
SELECT * FROM ProviderAudit WHERE ProviderId = 12 ORDER BY AuditId;

-- Maintenance: licenses still recorded as Active after their expiration date
SELECT LicenseId, ProviderName, LicenseNumber, ExpirationDate
FROM vw_LicenseEffectiveStatus
WHERE IsStatusStale = 1;

-- Maintenance: licenses expiring in the next 60 days
SELECT ProviderName, LicenseNumber, ExpirationDate, DaysUntilExpiration
FROM vw_LicenseEffectiveStatus
WHERE EffectiveStatus = 'Active' AND DaysUntilExpiration BETWEEN 0 AND 60
ORDER BY DaysUntilExpiration;

-- Restore a soft-deleted provider (prefer POST /api/providers/{id}/restore)
-- UPDATE Provider
-- SET IsDeleted = 0, DeletedDate = NULL, DeletedBy = NULL,
--     UpdatedDate = strftime('%Y-%m-%d %H:%M:%S', 'now'), UpdatedBy = 'support', Version = Version + 1
-- WHERE ProviderId = 12;
