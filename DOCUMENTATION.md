# HKERP - Detailed Documentation

This document explains the **exact role of every file** and the **step-by-step code flow**
(from request coming in to response going out) for each API. For a quick setup guide, see
`README.md`; this file is for a deeper understanding of the codebase.

---

## 1. Layers - who depends on whom

```
HKERP.API            (controllers, filters, startup)
    │
    ├──▶ HKERP.Application   (business logic, interfaces, DTOs)
    │        │
    │        └──▶ HKERP.Domain   (no dependencies - pure helpers/entities)
    │
    └──▶ HKERP.Infrastructure   (DB access - ADO.NET + EF Core)
             │
             └──▶ HKERP.Application, HKERP.Domain
```

Rule: **Domain** is the innermost layer and depends on nothing. **Application** holds business
rules but knows nothing about the DB/HTTP (only interfaces). **Infrastructure** implements
those interfaces (talks to SQL Server). **API** wires everything together (via Dependency
Injection) and handles HTTP requests.

---

## 2. File-by-file (what each one does)

### HKERP.Domain (no dependencies)

| File | Role |
|---|---|
| `Common/CommonMethods.cs` | Two jobs: (1) `ConvertDataTableToList`/`ConvertDataSetToList` - converts a SQL `DataTable`/`DataSet` into a `Dictionary<string,object>` list, so no fixed C# class needs to be created and JSON serialization also works correctly. (2) `Encrypt`/`Decrypt` - the same TripleDES(ECB,PKCS7) + MD5-hashed-key logic as the old MVC project, for encrypting/decrypting passwords. |
| `Common/DateTimeHelper.cs` | `GetIstNow()` - always returns the current time in **India Standard Time**, no matter where the server itself is hosted (any country/timezone). Access-control's time-window and daily-limit checks depend on this. |
| `Entities/ApiAccessControl.cs` | Plain C# shape (POCO) for the `MST_ApiAccessControl` SQL table - EF Core maps this class to that table. Fields: `Ledger_ID`, `ApiName`, `MaxCallsPerDay`, `AllowedFromTime`, `AllowedToTime`, `IsActive`. |
| `Entities/ApiAccessLog.cs` | POCO for the `TRN_ApiAccessLog` SQL table - a record of every API call (start/end time, response time in ms, success/fail, message). |

### HKERP.Application (business logic + contracts)

| File | Role |
|---|---|
| `DTOs/LoginRequestDto.cs` | Shape of the login request body: `UserName`, `Password`, `DeviceType`, `IP`. |
| `DTOs/FaithLabMnlPrcRequestDto.cs` | Shape of the FaithLab API request body: `From`, `To` (required), `UniqueId` (optional). |
| `DTOs/ApiResponseDto.cs` | The common response shape used by **every** API: `{ Success, Message, Data }`. `Data` is an object, so a result of any shape (dynamic) fits into it. |
| `Interfaces/ILoginRepository.cs` | Contract - "something that validates username/password against the DB and returns a `DataSet`". Infrastructure implements this. |
| `Interfaces/ILoginService.cs` | Contract - "something that performs the login business logic". `AuthController` talks only to this interface, it doesn't know about the implementation (`LoginService`). |
| `Interfaces/ITokenService.cs` | Contract - "something that generates a JWT token". |
| `Interfaces/IGenericSpService.cs` | Contract for the **common SP-caller**: "give it any SP name (and optionally XML), get back a Dictionary list". This is what lets new APIs be built without writing a new repository each time. |
| `Interfaces/IApiAccessRepository.cs` | Contract - checking the access-control table + writing logs. `GetAccessControl`, `GetTodaySuccessCallCount`, `LogAccessStart`, `LogAccessEnd`. |
| `Services/LoginService.cs` | **The entire login business logic**: encrypt the password -> call the SP -> check the SP's result (explicit failure / no rows / success) -> as soon as `Ledger_ID` is known, manually run the access-control check (because there's no JWT token yet) -> if everything passes, generate the token and log the call. Detailed flow is in section 3 below. |
| `Services/TokenService.cs` | Generates a JWT token using `appsettings.json`'s `JwtSettings` (SecretKey, Issuer, Audience, ExpiryMinutes). Embeds `Sub` (userId), `UserName`, `LedgerId` claims in the token - the `LedgerId` claim is what the access-control filter uses downstream. |

### HKERP.Infrastructure (DB access - ADO.NET + EF Core)

| File | Role |
|---|---|
| `Common/SqlHelper.cs` | Raw ADO.NET wrapper. `ExecuteSpWithXml(xml, spName)` - calls an SP with an `@XML` parameter (the old pattern). `ExecuteSpNoParams(spName)` - calls an SP with no parameters at all (e.g. `RPT_PRC_DMTX_INV_API`). Both return a `DataSet`. |
| `Data/ControlDbContext.cs` | EF Core `DbContext` - **only** for the `MST_ApiAccessControl` and `TRN_ApiAccessLog` tables (because these needed LINQ querying - counting, filtering, etc). The rest of the project runs entirely on SPs/ADO.NET. |
| `Repositories/LoginRepository.cs` | Implementation of `ILoginRepository` - builds the XML (`<DocumentElement><username>...</username>...`), calls the `HKERP_USER_LOGIN` SP via `SqlHelper.ExecuteSpWithXml`. |
| `Repositories/ApiAccessRepository.cs` | Implementation of `IApiAccessRepository` - runs LINQ queries against `ControlDbContext`: `GetAccessControl` (find the row), `GetTodaySuccessCallCount` (count today's successful calls), `LogAccessStart`/`LogAccessEnd` (insert/update a log row). |
| `Services/GenericSpService.cs` | Implementation of `IGenericSpService` - uses `SqlHelper` to call an SP (handles both with-xml and without-xml cases), then converts the result into a Dictionary list via `CommonMethods.ConvertDataTableToList`/`ConvertDataSetToList`. |

### HKERP.API (entry point, HTTP layer)

| File | Role |
|---|---|
| `Program.cs` | The application's **startup** - registers all services in the DI container (`SqlHelper`, repositories, services, `ControlDbContext`), configures JWT authentication, sets up Swagger, builds the middleware pipeline (exception handler -> auth -> controllers). |
| `appsettings.json` | Configuration values: SQL connection string, `SecurityKey` (for password encryption), `JwtSettings` (for generating tokens). |
| `Controllers/AuthController.cs` | `POST /api/Auth/login` - purely an HTTP layer, delegates all the work to `ILoginService.Login()`, wraps the result in `Ok()`/`500`. |
| `Controllers/DmtxController.cs` | `GET /api/Dmtx/prc-dmtx-inv` - `[Authorize]` (requires JWT) + `[ApiAccessControl("PRC_DMTX_INV_API")]` (access-check happens automatically). Calls `IGenericSpService.ExecuteSp("RPT_PRC_DMTX_INV_API")` and returns the result. |
| `Controllers/FaithLabController.cs` | `POST /api/FaithLab/mnl-prc` - validates `From`/`To` (required), builds the XML, access-check happens via `[ApiAccessControl("FaithLab_MnlPrc")]`, calls `IGenericSpService.ExecuteSp("Get_FaithLab_MnlPrc_Send_Surat_API", xml)`. |
| `Filters/ApiAccessControlAttribute.cs` | **Reusable access-control filter** - just add `[ApiAccessControl("API_NAME")]` on any controller action. Extracts the user from the JWT's `LedgerId` claim -> checks `MST_ApiAccessControl` (does a row exist? within the time-window? daily-limit remaining?) -> only lets the actual action run if everything passes -> after the action runs, writes the full record to `TRN_ApiAccessLog` (how long it took, success/fail). **The Login API does not use this** (because there's no token yet before login) - there the check is done manually inside `LoginService.cs`. |

### SqlScripts (database setup)

| File | Role |
|---|---|
| `01_Create_Access_Control_Tables.sql` | Creates the `MST_ApiAccessControl` and `TRN_ApiAccessLog` tables. |
| `02_Sample_Access_Grant.sql` | Example: granting `PRC_DMTX_INV_API` access to a user. |
| `03_FaithLab_Access_Grant.sql` | Example: granting `FaithLab_MnlPrc` access. |
| `04_Login_Access_Grant.sql` | Example: granting `Login` access (**without this, nobody can log in**). |

---

## 3. Code Flow - Request to Response (each API)

### 3.1 Login - `POST /api/Auth/login`

```
1. Client calls POST /api/Auth/login (from Postman/Swagger)
   body: { userName, password, deviceType, ip }
   |
2. AuthController.Login() receives the request
   |
3. AuthController -> ILoginService.Login(request)  (LoginService.cs)
   |
4. LoginService:
   a. Reads SecurityKey from appsettings.json
   b. CommonMethods.Encrypt(password, true, securityKey) -> encrypted password
   c. Calls ILoginRepository.ValidateUser(...)
      |
      LoginRepository: builds XML <DocumentElement><username>..</...>
      |
      ISqlHelper.ExecuteSpWithXml(xml, "HKERP_USER_LOGIN")
      |
      SqlHelper: calls the SP on SQL Server via raw ADO.NET, returns a DataSet
   |
5. LoginService checks the DataSet:
   - SP returned Success=false? -> return { success:false, message: <SP's message> }
   - No rows found? -> return { success:false, message: "Password Or Username Incorrect !" }
   - Row found -> build a Dictionary list via CommonMethods.ConvertDataTableToList()
   |
6. Extracts Ledger_ID from the row -> IApiAccessRepository.GetAccessControl(ledgerId, "Login")
   - No row found? -> return { success:false, message: "access not granted" }
   - Outside the time-window? -> return { success:false, message: "...allowed only between..." }
   - Daily limit exhausted? -> return { success:false, message: "...limit reached" }
   |
7. Everything passes -> ITokenService.GenerateToken(userId, userName, ledgerId) -> JWT token
   |
8. IApiAccessRepository.LogAccessStart(log) -> inserts a row into TRN_ApiAccessLog (IsSuccess=true)
   |
9. return { success:true, message:"Login Successful", data:{ token, userDetails } }
   |
10. AuthController -> Ok(result) -> JSON response goes back to the client
```

### 3.2 Dmtx (2nd API) - `GET /api/Dmtx/prc-dmtx-inv`

```
1. Client calls GET /api/Dmtx/prc-dmtx-inv, header: Authorization: Bearer <token>
   |
2. ASP.NET Core's JWT middleware (configured in Program.cs) verifies the token,
   populates HttpContext.User with the token's claims (UserName, LedgerId, etc)
   |
3. [Authorize] attribute check - is the token valid? If not, 401 Unauthorized, request stops here
   |
4. [ApiAccessControl("PRC_DMTX_INV_API")] filter runs (BEFORE the action):
   a. Extracts LedgerId from User.FindFirst("LedgerId")
   b. IApiAccessRepository.GetAccessControl(ledgerId, "PRC_DMTX_INV_API")
      - Not found -> 403 Forbidden, "Access not allowed"
   c. Is the current IST time within AllowedFromTime-AllowedToTime?
      - No -> 403 Forbidden
   d. Is today's successful-call count < MaxCallsPerDay?
      - No -> 429 Too Many Requests
   e. Everything passes -> inserts a row into TRN_ApiAccessLog (records start time) -> gets logId
   |
5. The actual controller action now runs: DmtxController.GetPrcDmtxInv()
   |
6. IGenericSpService.ExecuteSp("RPT_PRC_DMTX_INV_API")
   |
   GenericSpService: no xml given, so calls ISqlHelper.ExecuteSpNoParams(spName)
   |
   SqlHelper: calls the SP on SQL Server (with no parameters), gets back a DataSet
   |
   GenericSpService: builds a Dictionary list via CommonMethods.ConvertDataTableToList()
   |
7. DmtxController -> Ok({ success:true, message:"Success", data: result })
   |
8. Control returns to the filter (AFTER the action): calculates response time (Stopwatch),
   IApiAccessRepository.LogAccessEnd(logId, endTime, ms, success, message)
   -> the same TRN_ApiAccessLog row gets updated (end time, response time, success)
   |
9. Final JSON response goes back to the client
```

### 3.3 FaithLab_MnlPrc (3rd API) - `POST /api/FaithLab/mnl-prc`

```
1. Client calls POST /api/FaithLab/mnl-prc
   body: { from: "28-Dec-2025", to: "31-Dec-2025", uniqueId: "18169" }
   header: Authorization: Bearer <token>
   |
2. JWT middleware + [Authorize] check (same as Dmtx)
   |
3. FaithLabController.GetMnlPrc() validates From/To
   - Missing? -> 400 Bad Request, the controller action returns right here
     (NOTE: the [ApiAccessControl] filter has already run before the action -
      so even on a validation failure a log entry already exists, which gets
      updated at the end with IsSuccess based on the action's outcome)
   |
4. [ApiAccessControl("FaithLab_MnlPrc")] filter runs (exactly like Dmtx - see step 4
   in section 3.2): does access exist? within time-window? daily-limit remaining?
   -> if everything passes, log-start happens
   |
5. Controller builds the XML:
   <DocumentElement><form>28-Dec-2025</form><to>31-Dec-2025</to><UniqueId>18169</UniqueId></DocumentElement>
   |
6. IGenericSpService.ExecuteSp("Get_FaithLab_MnlPrc_Send_Surat_API", xml)
   |
   GenericSpService: xml is given, so calls ISqlHelper.ExecuteSpWithXml(xml, spName)
   |
   SqlHelper: calls the SP on SQL Server (with the @XML parameter), gets back a DataSet
   |
   GenericSpService: converts it into a Dictionary list
   |
7. Controller -> Ok({ success:true, message:"Success", data: result })
   |
8. The filter logs the end after the response (same as Dmtx)
   |
9. Final JSON response goes back to the client
```

---

## 4. Checklist for adding a new API (for the future)

1. Create a **DTO** (`Application/DTOs/`) - if POST/parameters are needed
2. Create a **Controller** (`API/Controllers/`) - inject `IGenericSpService` in the constructor
3. In the action, call `_spService.ExecuteSp("SP_NAME")` or `_spService.ExecuteSp("SP_NAME", xml)`
4. If access-control is needed, add `[Authorize]` + `[ApiAccessControl("NEW_API_NAME")]`
5. Insert a row for that user/API into `MST_ApiAccessControl` (SQL script)
6. Add the new API to the tables in this `DOCUMENTATION.md` and `README.md`

This means a new SqlHelper, converter, or access-control logic never needs to be written
again - just a DTO + Controller + SQL grant is needed.
