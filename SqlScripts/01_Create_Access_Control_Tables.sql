-- ============================================================
-- MST_ApiAccessControl : kis user (Ledger_ID) ko kaunsi API,
-- kitni baar/din, aur kaunse time-window (IST) me access hai
-- ============================================================
CREATE TABLE MST_ApiAccessControl
(
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    Ledger_ID         UNIQUEIDENTIFIER NOT NULL,
    ApiName           VARCHAR(200)     NOT NULL,
    MaxCallsPerDay    INT              NOT NULL,
    AllowedFromTime   TIME             NOT NULL,
    AllowedToTime     TIME             NOT NULL,
    IsActive          BIT              NOT NULL DEFAULT(1)
);

CREATE INDEX IX_MST_ApiAccessControl_LedgerId_ApiName
    ON MST_ApiAccessControl (Ledger_ID, ApiName);

-- ============================================================
-- TRN_ApiAccessLog : har API call ka record - start/end time,
-- response time, success/fail - sab IST me store hota hai
-- ============================================================
CREATE TABLE TRN_ApiAccessLog
(
    Id                BIGINT IDENTITY(1,1) PRIMARY KEY,
    Ledger_ID         UNIQUEIDENTIFIER NOT NULL,
    ApiName           VARCHAR(200)     NOT NULL,
    CallDateIST       DATE             NOT NULL,
    StartTimeIST      DATETIME2        NOT NULL,
    EndTimeIST        DATETIME2        NULL,
    ResponseTimeMs    INT              NULL,
    IsSuccess         BIT              NOT NULL DEFAULT(0),
    Message           NVARCHAR(1000)   NULL
);

CREATE INDEX IX_TRN_ApiAccessLog_LedgerId_ApiName_Date
    ON TRN_ApiAccessLog (Ledger_ID, ApiName, CallDateIST);
