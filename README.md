# HKERP - .NET Core Web API

Clean Architecture Web API jo purane HKESalesERP MVC project ke Stored-Procedure-based
pattern ko naye .NET 8 me continue karta hai, plus JWT auth + per-user API access-control
aur logging.

---

## Folder Structure (Tree)

```
HKERP/
├── HKERP.sln
├── README.md
├── SqlScripts/
│   ├── 01_Create_Access_Control_Tables.sql   <- MST_ApiAccessControl + TRN_ApiAccessLog
│   └── 02_Sample_Access_Grant.sql            <- example: user ko API access dena
│
├── HKERP.Domain/                              <- koi dependency nahi, sabse andar ki layer
│   ├── Common/
│   │   ├── CommonMethods.cs      <- DataTable→Dictionary converter, Encrypt/Decrypt (TripleDES)
│   │   └── DateTimeHelper.cs     <- IST (India Standard Time) current-time helper
│   └── Entities/
│       ├── ApiAccessControl.cs   <- MST_ApiAccessControl table ka POCO (EF Core)
│       └── ApiAccessLog.cs       <- TRN_ApiAccessLog table ka POCO (EF Core)
│
├── HKERP.Application/                          <- business logic + contracts (interfaces)
│   ├── DTOs/
│   │   ├── LoginRequestDto.cs
│   │   └── ApiResponseDto.cs     <- sab API ka common response shape {success, message, data}
│   ├── Interfaces/
│   │   ├── ILoginRepository.cs
│   │   ├── ILoginService.cs
│   │   ├── ITokenService.cs
│   │   ├── IGenericSpService.cs  <- COMMON: koi bhi naya SP call karne ka interface
│   │   └── IApiAccessRepository.cs
│   └── Services/
│       ├── LoginService.cs       <- login business logic (encrypt password, SP call, JWT banao)
│       └── TokenService.cs       <- JWT token generate karta hai

├── HKERP.Infrastructure/                       <- DB access (ADO.NET + EF Core)
│   ├── Common/
│   │   └── SqlHelper.cs          <- raw ADO.NET se SP call karta hai (with-XML aur no-params dono)
│   ├── Data/
│   │   └── ControlDbContext.cs   <- EF Core DbContext SIRF access-control/log table ke liye
│   ├── Repositories/
│   │   ├── LoginRepository.cs    <- HKERP_USER_LOGIN SP call karta hai
│   │   └── ApiAccessRepository.cs <- LINQ se access-control check + log insert/update
│   └── Services/
│       └── GenericSpService.cs   <- COMMON: naya SP call karna ho, yehi use karo

└── HKERP.API/                                  <- entry point, controllers, filters
    ├── Program.cs                 <- DI registrations, JWT config, middleware pipeline
    ├── appsettings.json           <- connection string, SecurityKey, JWT settings
    ├── Controllers/
    │   ├── AuthController.cs      <- POST /api/Auth/login
    │   ├── DmtxController.cs      <- GET /api/Dmtx/prc-dmtx-inv (2nd API)
    │   └── FaithLabController.cs  <- POST /api/FaithLab/mnl-prc (3rd API)
    └── Filters/
        └── ApiAccessControlAttribute.cs  <- COMMON: [ApiAccessControl("API_NAME")] attribute
```

---

## Dependency Direction

```
API  ─→  Application  ─→  Domain
API  ─→  Infrastructure ─→  Application, Domain
```

Domain kisi pe depend nahi karta. Application sirf Domain pe. Infrastructure Application+Domain
pe (interfaces implement karne ke liye). API sabpe (DI wire-up ke liye).

---

## Setup

1. `HKERP.sln` VS 2022 me kholo, NuGet restore hone do.
2. `HKERP.API/appsettings.json` me `ConnectionStrings.HKERPConnection` me apna Server/User/Password bharo.
3. `SecurityKey` already `DnKTech123` set hai (purane Web.config se confirm kiya gaya hai) - change mat karna jab tak wo bhi change na ho.
4. `JwtSettings.SecretKey` ko production me ek random 32+ char string se replace karo.
5. `SqlScripts/01_Create_Access_Control_Tables.sql` DB me run karo (agar tables already nahi hai).
6. Kisi user ko kisi API ka access dena ho to `SqlScripts/02_Sample_Access_Grant.sql` jaisa INSERT chalao (Ledger_ID, ApiName, MaxCallsPerDay, From/To time IST).
7. `Ctrl+F5` run karo.

---

## API 1: Login - `POST /api/Auth/login`

```json
{
  "userName": "xx",
  "password": "xx",
  "deviceType": "HKERP",
  "ip": "string"
}
```

**Important - Login pe bhi access-control lagta hai, lekin alag tarike se:**
`[ApiAccessControl]` attribute use nahi ho sakta yaha, kyunki wo JWT token ke `LedgerId` claim
se user identify karta hai - aur Login khud token generate karta hai, isliye login ke waqt
koi token hota hi nahi. Isliye `LoginService.cs` ke andar hi manually check hota hai:
1. Pehle SP se username/password validate hota hai
2. Tabhi `Ledger_ID` milta hai
3. **Usi `Ledger_ID`** se `MST_ApiAccessControl` me `ApiName = 'Login'` wali row check hoti hai
   (time-window + daily-limit, bilkul waise hi jaise doosre APIs me)
4. Pass hone par hi token banta hai, aur call `TRN_ApiAccessLog` me log hota hai

**Iska matlab: agar kisi user ko `MST_ApiAccessControl` me `ApiName='Login'` wali row nahi
di gayi, wo login hi nahi kar payega**, chahe uska username/password bilkul sahi ho.
`SqlScripts/04_Login_Access_Grant.sql` me example hai.

Response me JWT token milta hai jisme `LedgerId` claim embedded hota hai - yahi claim aage
access-control filter use karta hai.

---

## API 2: PRC_DMTX_INV_API - `GET /api/Dmtx/prc-dmtx-inv`

- Koi request body/parameter nahi
- `Authorization: Bearer <token>` header chahiye (login se mila token)
- Andar `RPT_PRC_DMTX_INV_API` SP bina kisi parameter ke call hoti hai
- Response dynamic hai (jo bhi columns SP return kare, wahi aa jayenge)

### Access control isi endpoint pe kaise lagta hai

```csharp
[HttpGet("prc-dmtx-inv")]
[ApiAccessControl("PRC_DMTX_INV_API")]   // <- bas ye ek line
public IActionResult GetPrcDmtxInv() { ... }
```

`ApiAccessControlAttribute` (Filters/) automatically:
1. Token ke `LedgerId` claim se user identify karta hai
2. `MST_ApiAccessControl` table me check karta hai us user ko is API ("PRC_DMTX_INV_API") ka access hai ya nahi
3. Abhi current time (IST, chahe user duniya me kahi se bhi call kare) us row ke `AllowedFromTime`-`AllowedToTime` ke beech hai ya nahi
4. Aaj (IST date) ke successful calls ka count `MaxCallsPerDay` se kam hai ya nahi
5. Sab pass hone par hi actual API logic chalta hai; `TRN_ApiAccessLog` me start-time, end-time, response-time (ms), success/fail record ho jata hai automatically

Agar koi bhi check fail ho: `403 Forbidden` (access nahi / time window ke bahar) ya `429 Too Many Requests`
(daily limit khatam) response deta hai, actual SP call hota hi nahi.

---

## API 3: FaithLab_MnlPrc - `POST /api/FaithLab/mnl-prc`

```json
{
  "from": "xx",
  "to": "xx",
  "uniqueId": "xx"
}
```

- `from` aur `to` **compulsory** hai (missing hone par 400 Bad Request aata hai)
- `uniqueId` optional hai (comma-separated bhi ho sakta hai, SP khud `STRING_SPLIT` se handle karta hai)
- Andar `Get_FaithLab_MnlPrc_Send_Surat_API` SP ko XML parameter ke saath call karta hai
- `Authorization: Bearer <token>` header chahiye
- `[ApiAccessControl("FaithLab_MnlPrc")]` laga hai - access grant `SqlScripts/03_FaithLab_Access_Grant.sql` me hai

## Naya API add karna ho (future) - is pattern ko follow karo

1. Naya controller banao, jaisa `DmtxController.cs`
2. `IGenericSpService` inject karo (naya Repository/Service banane ki zaroorat nahi)
3. `_spService.ExecuteSp("YOUR_SP_NAME")` - bina parameter ke SP call
   ya `_spService.ExecuteSp("YOUR_SP_NAME", xmlString)` - XML parameter ke saath (purane pattern jaisa)
4. Access control chahiye ho to `[ApiAccessControl("YOUR_API_NAME")]` attribute lagao aur
   `MST_ApiAccessControl` me us user/API ke liye ek row insert kar do
5. Response hamesha `ApiResponseDto { Success, Message, Data }` shape me return karo

Isse har naye SP/API ke liye dobara SqlHelper, converter, ya access-control logic likhne ki
zaroorat nahi padegi - sab common/reusable hai.

---

## Important Notes

- `CommonMethods.ConvertDataTableToList` ab `Dictionary<string,object>` return karta hai
  (pehle Newtonsoft `dynamic`/JObject tha) - isse ASP.NET Core ka default JSON serializer
  (System.Text.Json) sahi se values serialize karta hai. Pehle ye bug tha ki `userDetails`
  me sab fields empty array `[]` dikhte the - ab fix hai.
- `ControlDbContext` (EF Core) SIRF access-control aur log table ke liye hai - baaki poora
  project ADO.NET + Stored Procedures se hi chalta hai, jaisa purane MVC project me tha.
  Naya business-data module add karte waqt EF entity mat banao, `GenericSpService` use karo.
- IST timezone `DateTimeHelper.GetIstNow()` se milta hai - Windows pe "India Standard Time"
  id use karta hai, Linux/Docker pe fallback "Asia/Kolkata" IANA id use karta hai.
