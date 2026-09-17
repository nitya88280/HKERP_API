-- Login API ke liye access dena - bina is row ke ab koi bhi user login nahi kar payega
-- (chahe username/password sahi ho), kyunki ab Login pe bhi access-control check lagta hai.
INSERT INTO MST_ApiAccessControl (Ledger_ID, ApiName, MaxCallsPerDay, AllowedFromTime, AllowedToTime, IsActive)
VALUES ('9E9EE41D-56D1-436B-AA1E-DB7E89417147', 'Login', 50, '00:00:00', '23:59:59', 1);
