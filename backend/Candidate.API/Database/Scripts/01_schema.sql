-- =============================================================================
-- 01_schema.sql
-- Tables, indexes and integrity triggers for the Provider / License system.
--
-- This script is the source of truth for the physical schema. The API maps
-- Entity Framework Core onto these tables; it does not generate them.
-- Run order: 01_schema.sql -> 02_views.sql -> 03_seed.sql
-- =============================================================================

PRAGMA foreign_keys = ON;

-- -----------------------------------------------------------------------------
-- Provider
-- Soft delete: IsDeleted flag + DeletedDate/DeletedBy. Rows are never removed.
-- Version is an optimistic-concurrency token incremented by the API on every
-- update, so two people editing the same provider cannot silently overwrite
-- each other.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Provider (
    ProviderId    INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderName  TEXT    NOT NULL CHECK (length(trim(ProviderName)) BETWEEN 1 AND 200),
    County        TEXT    NOT NULL CHECK (length(trim(County)) BETWEEN 1 AND 100),
    Status        TEXT    NOT NULL DEFAULT 'Pending'
                          CHECK (Status IN ('Active', 'Inactive', 'Pending')),
    CreatedDate   TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%S', 'now')),
    UpdatedDate   TEXT    NULL,
    UpdatedBy     TEXT    NULL,
    Version       INTEGER NOT NULL DEFAULT 1,
    IsDeleted     INTEGER NOT NULL DEFAULT 0 CHECK (IsDeleted IN (0, 1)),
    DeletedDate   TEXT    NULL,
    DeletedBy     TEXT    NULL,
    -- The flag and the deletion stamp must always agree.
    CHECK ((IsDeleted = 0 AND DeletedDate IS NULL AND DeletedBy IS NULL)
        OR (IsDeleted = 1 AND DeletedDate IS NOT NULL))
);

-- Standard queries always filter on IsDeleted (and usually Status).
CREATE INDEX IF NOT EXISTS IX_Provider_IsDeleted_Status ON Provider (IsDeleted, Status);
CREATE INDEX IF NOT EXISTS IX_Provider_County           ON Provider (County COLLATE NOCASE);

-- The same provider may not be registered twice in one county, but only among
-- live rows: a soft-deleted record must not block re-creating that provider.
-- (Partial index; also serves name lookups.)
CREATE UNIQUE INDEX IF NOT EXISTS UX_Provider_Name_County_NotDeleted
    ON Provider (ProviderName COLLATE NOCASE, County COLLATE NOCASE)
    WHERE IsDeleted = 0;

-- -----------------------------------------------------------------------------
-- License
-- A license always belongs to exactly one provider. ExpirationDate is stored as
-- ISO 'yyyy-MM-dd' text so string comparison equals date comparison.
-- LicenseStatus is the status recorded by staff; the effective status (which
-- also considers ExpirationDate) is computed in 02_views.sql.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS License (
    LicenseId      INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderId     INTEGER NOT NULL
                           REFERENCES Provider (ProviderId) ON DELETE RESTRICT ON UPDATE RESTRICT,
    LicenseNumber  TEXT    NOT NULL CHECK (length(trim(LicenseNumber)) BETWEEN 1 AND 50),
    LicenseStatus  TEXT    NOT NULL CHECK (LicenseStatus IN ('Active', 'Expired', 'Suspended')),
    ExpirationDate TEXT    NOT NULL
                           CHECK (ExpirationDate GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                              AND date(ExpirationDate) IS NOT NULL),
    CreatedDate    TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%S', 'now')),
    UpdatedDate    TEXT    NULL
);

-- "License number is usually unique per provider."
CREATE UNIQUE INDEX IF NOT EXISTS UX_License_ProviderId_LicenseNumber ON License (ProviderId, LicenseNumber);
CREATE INDEX IF NOT EXISTS IX_License_ExpirationDate ON License (ExpirationDate);

-- -----------------------------------------------------------------------------
-- ProviderAudit
-- Append-only history of every change to a provider, written by triggers so it
-- is captured no matter which client (API, script, DB tool) made the change.
-- Each row is a snapshot of the provider *after* the change.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ProviderAudit (
    AuditId      INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderId   INTEGER NOT NULL REFERENCES Provider (ProviderId),
    Action       TEXT    NOT NULL CHECK (Action IN ('Created', 'Updated', 'SoftDeleted', 'Restored')),
    ProviderName TEXT    NOT NULL,
    County       TEXT    NOT NULL,
    Status       TEXT    NOT NULL,
    IsDeleted    INTEGER NOT NULL,
    ChangedDate    TEXT    NOT NULL,
    ChangedBy    TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_ProviderAudit_ProviderId ON ProviderAudit (ProviderId, AuditId);

-- =============================================================================
-- Triggers
-- =============================================================================

-- Hard deletion is not allowed by organizational rule. Enforcing it in the
-- database means a stray DELETE from any tool fails, not just from the API.
CREATE TRIGGER IF NOT EXISTS TR_Provider_PreventHardDelete
BEFORE DELETE ON Provider
BEGIN
    SELECT RAISE(ABORT, 'Hard delete is not allowed on Provider. Set IsDeleted = 1 instead.');
END;

CREATE TRIGGER IF NOT EXISTS TR_License_PreventHardDelete
BEFORE DELETE ON License
BEGIN
    SELECT RAISE(ABORT, 'Hard delete is not allowed on License.');
END;

-- A soft-deleted provider is read-only until it is restored.
CREATE TRIGGER IF NOT EXISTS TR_Provider_PreventEditWhileDeleted
BEFORE UPDATE ON Provider
WHEN OLD.IsDeleted = 1 AND NEW.IsDeleted = 1
BEGIN
    SELECT RAISE(ABORT, 'Provider is soft-deleted. Restore it before making changes.');
END;

-- New licenses cannot be attached to a soft-deleted provider.
CREATE TRIGGER IF NOT EXISTS TR_License_PreventInsertForDeletedProvider
BEFORE INSERT ON License
WHEN (SELECT IsDeleted FROM Provider WHERE ProviderId = NEW.ProviderId) = 1
BEGIN
    SELECT RAISE(ABORT, 'Cannot add a license to a soft-deleted provider.');
END;

-- Audit history is append-only.
CREATE TRIGGER IF NOT EXISTS TR_ProviderAudit_PreventUpdate
BEFORE UPDATE ON ProviderAudit
BEGIN
    SELECT RAISE(ABORT, 'ProviderAudit is append-only.');
END;

CREATE TRIGGER IF NOT EXISTS TR_ProviderAudit_PreventDelete
BEFORE DELETE ON ProviderAudit
BEGIN
    SELECT RAISE(ABORT, 'ProviderAudit is append-only.');
END;

CREATE TRIGGER IF NOT EXISTS TR_Provider_AuditInsert
AFTER INSERT ON Provider
BEGIN
    INSERT INTO ProviderAudit (ProviderId, Action, ProviderName, County, Status, IsDeleted, ChangedDate, ChangedBy)
    VALUES (NEW.ProviderId, 'Created', NEW.ProviderName, NEW.County, NEW.Status, NEW.IsDeleted,
            NEW.CreatedDate, COALESCE(NEW.UpdatedBy, 'system'));
END;

CREATE TRIGGER IF NOT EXISTS TR_Provider_AuditUpdate
AFTER UPDATE ON Provider
BEGIN
    INSERT INTO ProviderAudit (ProviderId, Action, ProviderName, County, Status, IsDeleted, ChangedDate, ChangedBy)
    VALUES (NEW.ProviderId,
            CASE
                WHEN OLD.IsDeleted = 0 AND NEW.IsDeleted = 1 THEN 'SoftDeleted'
                WHEN OLD.IsDeleted = 1 AND NEW.IsDeleted = 0 THEN 'Restored'
                ELSE 'Updated'
            END,
            NEW.ProviderName, NEW.County, NEW.Status, NEW.IsDeleted,
            COALESCE(NEW.DeletedDate, NEW.UpdatedDate, strftime('%Y-%m-%d %H:%M:%S', 'now')),
            COALESCE(NEW.DeletedBy, NEW.UpdatedBy, 'system'));
END;
