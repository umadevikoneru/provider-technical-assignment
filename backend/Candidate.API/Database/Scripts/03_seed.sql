-- =============================================================================
-- 03_seed.sql
-- Sample data that exercises every required scenario. Expiration dates are
-- relative to the day the script runs, so the scenarios stay true over time.
--
--   Scenario                                         Providers
--   -----------------------------------------------  ---------------------------
--   Active provider, only active licenses            1, 7, 8
--   Active provider with an expired license          2, 3, 10
--   Active provider, suspended license               6
--   Inactive provider that still holds a valid one   9
--   Pending provider without licenses                5
--   Soft-deleted provider (must not appear)          12
-- =============================================================================

INSERT INTO Provider (ProviderId, ProviderName, County, Status, CreatedDate, UpdatedBy) VALUES
    (1,  'Bright Horizons Early Learning',  'Fulton',   'Active',   strftime('%Y-%m-%d %H:%M:%S', 'now', '-400 days'), 'seed'),
    (2,  'Little Explorers Academy',        'DeKalb',   'Active',   strftime('%Y-%m-%d %H:%M:%S', 'now', '-380 days'), 'seed'),
    (3,  'Sunshine Kids Care',              'Cobb',     'Active',   strftime('%Y-%m-%d %H:%M:%S', 'now', '-360 days'), 'seed'),
    (4,  'Peachtree Child Development',     'Gwinnett', 'Inactive', strftime('%Y-%m-%d %H:%M:%S', 'now', '-900 days'), 'seed'),
    (5,  'Happy Trails Preschool',          'Clayton',  'Pending',  strftime('%Y-%m-%d %H:%M:%S', 'now', '-10 days'),  'seed'),
    (6,  'Savannah Tots Learning Center',   'Chatham',  'Active',   strftime('%Y-%m-%d %H:%M:%S', 'now', '-300 days'), 'seed'),
    (7,  'Augusta Family Daycare',          'Richmond', 'Active',   strftime('%Y-%m-%d %H:%M:%S', 'now', '-250 days'), 'seed'),
    (8,  'Macon Montessori School',         'Bibb',     'Active',   strftime('%Y-%m-%d %H:%M:%S', 'now', '-200 days'), 'seed'),
    (9,  'Columbus Kids Corner',            'Muscogee', 'Inactive', strftime('%Y-%m-%d %H:%M:%S', 'now', '-150 days'), 'seed'),
    (10, 'Athens Learning Tree',            'Clarke',   'Active',   strftime('%Y-%m-%d %H:%M:%S', 'now', '-500 days'), 'seed'),
    (11, 'Gainesville Growing Minds',       'Hall',     'Pending',  strftime('%Y-%m-%d %H:%M:%S', 'now', '-30 days'),  'seed'),
    (12, 'Henry County Kids Club',          'Henry',    'Active',   strftime('%Y-%m-%d %H:%M:%S', 'now', '-450 days'), 'seed');

INSERT INTO License (ProviderId, LicenseNumber, LicenseStatus, ExpirationDate, CreatedDate) VALUES
    -- 1: two valid licenses, one expiring soon
    (1,  'GA-FUL-1001', 'Active',    date('now', '+300 days'), strftime('%Y-%m-%d %H:%M:%S', 'now', '-400 days')),
    (1,  'GA-FUL-1002', 'Active',    date('now', '+45 days'),  strftime('%Y-%m-%d %H:%M:%S', 'now', '-320 days')),
    -- 2: still recorded Active, but past its expiration date (stale status)
    (2,  'GA-DEK-2001', 'Active',    date('now', '-30 days'),  strftime('%Y-%m-%d %H:%M:%S', 'now', '-380 days')),
    -- 3: one valid license and one correctly recorded as Expired
    (3,  'GA-COB-3001', 'Active',    date('now', '+200 days'), strftime('%Y-%m-%d %H:%M:%S', 'now', '-160 days')),
    (3,  'GA-COB-3002', 'Expired',   date('now', '-90 days'),  strftime('%Y-%m-%d %H:%M:%S', 'now', '-360 days')),
    -- 4: inactive provider with a long-expired license
    (4,  'GA-GWI-4001', 'Expired',   date('now', '-400 days'), strftime('%Y-%m-%d %H:%M:%S', 'now', '-900 days')),
    -- 6: suspended license (not expired by date)
    (6,  'GA-CHA-6001', 'Suspended', date('now', '+120 days'), strftime('%Y-%m-%d %H:%M:%S', 'now', '-300 days')),
    -- 7: valid, expiring within 30 days
    (7,  'GA-RIC-7001', 'Active',    date('now', '+20 days'),  strftime('%Y-%m-%d %H:%M:%S', 'now', '-250 days')),
    -- 8: valid
    (8,  'GA-BIB-8001', 'Active',    date('now', '+500 days'), strftime('%Y-%m-%d %H:%M:%S', 'now', '-200 days')),
    -- 9: inactive provider, license still valid (provider status != license validity)
    (9,  'GA-MUS-9001', 'Active',    date('now', '+150 days'), strftime('%Y-%m-%d %H:%M:%S', 'now', '-150 days')),
    -- 10: expired a few days ago, still recorded Active
    (10, 'GA-CLA-1001', 'Active',    date('now', '-5 days'),   strftime('%Y-%m-%d %H:%M:%S', 'now', '-500 days')),
    -- 11: pending provider with a license issued ahead of opening
    (11, 'GA-HAL-1101', 'Active',    date('now', '+365 days'), strftime('%Y-%m-%d %H:%M:%S', 'now', '-30 days')),
    -- 12: provider is soft-deleted below; neither license may appear in standard results
    (12, 'GA-HEN-1201', 'Active',    date('now', '+100 days'), strftime('%Y-%m-%d %H:%M:%S', 'now', '-450 days')),
    (12, 'GA-HEN-1202', 'Active',    date('now', '-10 days'),  strftime('%Y-%m-%d %H:%M:%S', 'now', '-450 days'));

-- Soft-delete provider 12 the same way the API does, so the audit trail shows it.
UPDATE Provider
SET IsDeleted = 1,
    DeletedDate = strftime('%Y-%m-%d %H:%M:%S', 'now', '-7 days'),
    DeletedBy = 'seed',
    UpdatedDate = strftime('%Y-%m-%d %H:%M:%S', 'now', '-7 days'),
    UpdatedBy = 'seed',
    Version   = Version + 1
WHERE ProviderId = 12;
