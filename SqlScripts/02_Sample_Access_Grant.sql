-- Example: user 5291 (Ledger_ID = 9E9EE41D-56D1-436B-AA1E-DB7E89417147) ko
-- PRC_DMTX_INV_API ka access - din me max 10 baar, sirf 7:00 AM se 5:30 PM (IST) ke beech
INSERT INTO MST_ApiAccessControl (Ledger_ID, ApiName, MaxCallsPerDay, AllowedFromTime, AllowedToTime, IsActive)
VALUES ('9E9EE41D-56D1-436B-AA1E-DB7E89417147', 'PRC_DMTX_INV_API', 10, '07:00:00', '17:30:00', 1);
