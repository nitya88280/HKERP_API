-- Example: kisi user ko FaithLab_MnlPrc API ka access dena
-- Apna real Ledger_ID, MaxCallsPerDay, aur allowed time-window (IST) daalo
INSERT INTO MST_ApiAccessControl (Ledger_ID, ApiName, MaxCallsPerDay, AllowedFromTime, AllowedToTime, IsActive)
VALUES ('9E9EE41D-56D1-436B-AA1E-DB7E89417147', 'FaithLab_MnlPrc', 10, '07:00:00', '17:30:00', 1);
