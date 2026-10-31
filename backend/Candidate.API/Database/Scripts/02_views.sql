-- =============================================================================
-- 02_views.sql
-- Reusable read models. Every "standard" view excludes soft-deleted providers,
-- so reports, the API and ad-hoc support queries all apply the same rule.
-- Safe to re-run: each view is dropped and recreated.
-- =============================================================================

DROP VIEW IF EXISTS vw_ActiveProvidersWithExpiredLicenses;
DROP VIEW IF EXISTS vw_ActiveProvidersActiveLicenses;
DROP VIEW IF EXISTS vw_LicenseEffectiveStatus;
DROP VIEW IF EXISTS vw_DeletedProviders;
DROP VIEW IF EXISTS vw_ActiveProviders;

-- -----------------------------------------------------------------------------
-- vw_ActiveProviders
-- "Active" here means "not soft-deleted" (the standard view of providers),
-- regardless of the provider's business Status.
-- -----------------------------------------------------------------------------
CREATE VIEW vw_ActiveProviders AS
SELECT ProviderId,
       ProviderName,
       County,
       Status,
       CreatedDate,
       UpdatedDate
FROM Provider
WHERE IsDeleted = 0;

-- -----------------------------------------------------------------------------
-- vw_DeletedProviders
-- Audit / troubleshooting view of soft-deleted providers.
-- -----------------------------------------------------------------------------
CREATE VIEW vw_DeletedProviders AS
SELECT ProviderId,
       ProviderName,
       County,
       Status,
       CreatedDate,
       DeletedDate,
       DeletedBy
FROM Provider
WHERE IsDeleted = 1;

-- -----------------------------------------------------------------------------
-- vw_LicenseEffectiveStatus
-- One row per license of a non-deleted provider, with the status the business
-- should act on. License expiration is determined by ExpirationDate:
--   * Suspended (recorded)              -> Suspended
--   * ExpirationDate before today       -> Expired   (valid through the expiration date itself)
--   * Expired (recorded, date in future) -> Expired   (staff ended it early)
--   * otherwise                         -> Active
-- IsStatusStale flags rows still recorded as Active after their expiration date,
-- which is a data-maintenance task for support staff.
-- Dates are evaluated in UTC (SQLite 'now').
-- -----------------------------------------------------------------------------
CREATE VIEW vw_LicenseEffectiveStatus AS
SELECT l.LicenseId,
       l.ProviderId,
       p.ProviderName,
       p.County,
       p.Status AS ProviderStatus,
       l.LicenseNumber,
       l.LicenseStatus AS RecordedStatus,
       l.ExpirationDate,
       CASE
           WHEN l.LicenseStatus = 'Suspended'     THEN 'Suspended'
           WHEN l.ExpirationDate < date('now')    THEN 'Expired'
           WHEN l.LicenseStatus = 'Expired'       THEN 'Expired'
           ELSE 'Active'
       END AS EffectiveStatus,
       CAST(julianday(l.ExpirationDate) - julianday(date('now')) AS INTEGER) AS DaysUntilExpiration,
       CASE
           WHEN l.LicenseStatus = 'Active' AND l.ExpirationDate < date('now') THEN 1
           ELSE 0
       END AS IsStatusStale
FROM License l
JOIN vw_ActiveProviders p ON p.ProviderId = l.ProviderId;

-- -----------------------------------------------------------------------------
-- Scenario 1: active providers and their active licenses.
-- -----------------------------------------------------------------------------
CREATE VIEW vw_ActiveProvidersActiveLicenses AS
SELECT ProviderId,
       ProviderName,
       County,
       LicenseId,
       LicenseNumber,
       ExpirationDate,
       DaysUntilExpiration
FROM vw_LicenseEffectiveStatus
WHERE ProviderStatus = 'Active'
  AND EffectiveStatus = 'Active';

-- -----------------------------------------------------------------------------
-- Scenario 2: providers that appear active but hold expired licenses.
-- One row per expired license; a provider may also hold other, valid licenses.
-- -----------------------------------------------------------------------------
CREATE VIEW vw_ActiveProvidersWithExpiredLicenses AS
SELECT ProviderId,
       ProviderName,
       County,
       LicenseId,
       LicenseNumber,
       RecordedStatus,
       ExpirationDate,
       -DaysUntilExpiration AS DaysSinceExpiration,
       IsStatusStale
FROM vw_LicenseEffectiveStatus
WHERE ProviderStatus = 'Active'
  AND EffectiveStatus = 'Expired';
