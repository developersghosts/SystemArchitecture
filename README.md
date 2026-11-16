# System Architecture

> English first, then Arabic (العربية بعد الإنجليزية).

---

## English

### Overview
A .NET 10 solution that hosts microservices. Each service follows **Clean Architecture** with four layers. The solution currently contains one service: `servicesA`.

### Folder structure

```
src/
├── SystemArchitecture.slnx     # Solution file
├── docker-compose.yml          # Runs the services in containers
├── Directory.Build.props       # Shared settings: net10.0, Nullable, ImplicitUsings
├── global.json                 # Pins the .NET SDK version
├── dotnet-tools.json           # Local tools (dotnet-ef 10.0.14)
├── .gitignore / .dockerignore  # Ignore rules
├── BuildingBlocks/             # Shared code used across services
│   ├── BuildingBlocks/               # CQRS, MediatR behaviors, exceptions + handler, options
│   ├── BuildingBlocks.Localization/  # Accept-Language localization (en/ar .resx)
│   └── BuildingBlocks.Messaging/     # MassTransit + RabbitMQ, integration events
├── UI/                         # Angular 21 front end (see "Angular UI")
└── Services/
    └── servicesA/
        ├── ServicesA.API                 # Program.cs, GlobalUsing.cs, ServiceInjection.cs (AddApi)
        ├── ServicesA.Application         # GlobalUsing.cs, ServiceInjection.cs (AddApplication)
        │   └── Abstractions/             # IAppDbContext
        ├── ServicesA.Domain
        │   ├── Common/                   # Entity, AggregateRoot, ValueObject, domain events, IAuditableEntity
        │   ├── Auditing/                 # AuditLog, LoginHistory, AuditAction
        │   └── Entities/Identity/        # ApplicationUser/Role/UserRole/UserClaim/UserLogin/UserToken/RoleClaim, FullName, Events/
        └── ServicesA.Infrastructure      # GlobalUsing.cs, ServiceInjection.cs (AddInfrastructure)
            └── Persistence/              # AppDbContext, Configurations/ (one per entity), Migrations/ (pending)
```

### Root files
| File | Purpose |
|------|---------|
| `SystemArchitecture.slnx` | The solution; groups all projects. |
| `docker-compose.yml` | Builds and runs `servicesa-api` (host port 5000 -> container port 8080) and `sqlserver` (SQL Server 2025, host port 1434 -> container 1433, data in the `sqlserver-data` volume). The API waits for SQL Server's healthcheck and gets its connection string from `ConnectionStrings__DefaultConnection`. The `sa` password comes from `MSSQL_SA_PASSWORD` (set in the Git-ignored `.env` file; falls back to a development-only default if `.env` is missing). The `sa` login is enabled by default in the container. |
| `Directory.Build.props` | Applies `net10.0`, nullable reference types and implicit usings to every project. |
| `global.json` | Pins the SDK to 10.0.x (rolls forward to the latest feature band). |
| `dotnet-tools.json` | Tool manifest that pins `dotnet-ef` 10.0.14 (run with `dotnet dotnet-ef`). |
| `.gitignore` | Excludes `bin/`, `obj/`, `.vs/`, `.idea/`, `*.user` from Git. |
| `.dockerignore` | Excludes `bin`, `obj`, `UI/`, `node_modules`, `.vs`, `.git` and `logs` from the Docker build context. |

### Clean Architecture layers (servicesA)
| Layer | Responsibility | References |
|-------|----------------|------------|
| **Domain** | Entities, value objects, domain rules. No dependencies. | none |
| **Application** | Use cases, interfaces, DTOs, business workflows. | Domain |
| **Infrastructure** | Implementations of external concerns: database, messaging, external APIs. | Application |
| **API** | Entry point (ASP.NET Core Web API), HTTP endpoints, DI composition. | Application, Infrastructure |

Dependencies point inward: API -> Infrastructure -> Application -> Domain.

### API project
- Uses **top-level statements** ([Program.cs](Services/servicesA/ServicesA.API/Program.cs)), with no `Main` method or `Program` class.
- Exposes OpenAPI in the Development environment.
- Has a simple `GET /` endpoint returning `ServicesA is running`.
- Includes a `Dockerfile` that builds with the .NET 10 SDK image and runs on the ASP.NET 10 runtime image.
- Registers everything in [Program.cs](Services/servicesA/ServicesA.API/Program.cs) with `AddApplication()`, `AddInfrastructure(configuration)` and `AddApi()`, then calls `app.MapCarter()`.

### Packages and where they live
| Layer | Package | Version | Registered in |
|-------|---------|---------|---------------|
| API | Carter | 10.0.0 | `AddApi()` -> `AddCarter()` |
| API | Serilog.AspNetCore | 10.0.0 | `AddSerilogLogging()` -> `UseSerilog(...)` reading the `Serilog` section; `UseSerilogRequestLogging()` |
| API | Microsoft.EntityFrameworkCore.Design | 10.0.14 | design-time only (migrations) |
| Application | MediatR | 15.0.0 | `AddApplication()` -> `AddMediatR(...)` |
| Application | FluentValidation.DependencyInjectionExtensions | 12.2.0 | `AddApplication()` -> `AddValidatorsFromAssembly(...)` |
| Application | Mapster + Mapster.DependencyInjection | 10.0.14 | `AddApplication()` -> scans mappings, registers `IMapper` |
| Infrastructure | Microsoft.EntityFrameworkCore.SqlServer | 10.0.14 | `AddInfrastructure()` -> `AddDbContext<AppDbContext>` |
| Infrastructure | System.Security.Cryptography.Pkcs | 10.0.14 | pinned directly to override the vulnerable 9.0.11 pulled in by `Microsoft.Data.SqlClient` (fixes NU1902/NU1903) |
| Infrastructure | Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.14 | `AddInfrastructure()` -> `AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<AppDbContext>()` |
| Application | Microsoft.EntityFrameworkCore | 10.0.14 | `DbSet` types in `IAppDbContext` |
| Domain | Microsoft.Extensions.Identity.Stores | 10.0.14 | base types `IdentityUser<Guid>` / `IdentityRole<Guid>` (no EF dependency) |

Carter and EF Core are pinned to net10-compatible versions because their latest releases (11.x) target net11 only.

Each of API, Application and Infrastructure has two helper files:
- `GlobalUsing.cs`: global using directives for that layer.
- `ServiceInjection.cs`: the `Add...` extension method that registers the layer's services.

The connection string `DefaultConnection` is in [appsettings.json](Services/servicesA/ServicesA.API/appsettings.json) (SQL Server LocalDB by default). `AppDbContext` is in `ServicesA.Infrastructure/Persistence`.

### Change log
| Change | Details |
|--------|---------|
| Message dictionary by dialect + Toastr | **Dictionary:** new table `LocalizedTexts` (migration `AddMessageDictionary`) with every backend message (API messages and emails, 42 keys) in English, Egyptian (`ar-EG`) and Saudi (`ar-SA`) Arabic, filled at startup only where missing (`Persistence/Seed/LocalizedTextSeed`), so edits are kept. New company field **Country** (`EG` / `SA` / empty = standard Arabic) in Project configuration decides the Arabic dialect. `BuildingBlocks.Localization` gained `ILocalizedTextStore`: `StringLocalizerService` reads the company dialect, then the language, then the .resx files. `DbLocalizedTextStore` keeps the whole dictionary in memory and reloads after a save. New page Settings > Message dictionary (`GET`/`PUT /api/Configuration/messages`, Administrator). **Toastr:** `ngx-toastr` 20.0.5; `ToastService` now shows Toastr notifications (themed, top corner, mirrored for Arabic); the fixed message boxes on login, code, forgot/reset password, dialogs, account and email pages are now toasts, and "signed in on another device" is a confirmation dialog. **Audit fix:** `AuditLogs` now also record value-object fields (e.g. `Company.NameAr`, `Currency.Symbol`); long values such as the logo are shortened. Checked: dialect switch (standard / Egyptian / Saudi / English, messages and email subjects), editing a text applies at once, 10 browser checks for toasts and the dictionary page. |
| New email design from the company settings | Every system email (OTP, login notice, welcome, reset password, password changed, test) uses one new template (`Application/Features/Identity/Common/EmailTemplates`, now a service) built from Project configuration in the database through `EmailBrandingProvider` (cached 10 minutes, cleared when the configuration is saved): main color (top bar, code box, button, links), light background, Arabic/English font, logo (sent inside the email as `cid:company-logo`, because Gmail/Outlook block `data:` images), company name in the email language, and address/phone/email/website in the footer when filled in. Codes are shown big and spaced; buttons work in Outlook and have the plain link under them; a plain-text part is added; layout flips for Arabic. Logos are now saved as PNG when possible (Outlook for Windows can't show WEBP). Checked with Mailpit: Arabic and English test emails, OTP and reset-password emails. |
| Email server settings saved in the database | New table `EmailServerSettings` (migration `AddEmailServerSettings`), edited in Settings > Email (`GET`/`PUT /api/Configuration/email-settings`, Administrator). The SMTP password is encrypted with ASP.NET Data Protection (`ISecretProtector`) before it is saved; the keys are kept outside the database in the docker volume `dataprotection-keys` (`DataProtection:KeysPath`), so a database copy alone does not reveal it — this also keeps reset links working after a restart. The API never returns the password (only `hasPassword`); empty = keep it; changing the server or user name requires typing it again (`SMTP_PASSWORD_REQUIRED`); it is excluded from AuditLogs and logs. `IEmailSettingsProvider` reads the database first and caches for 10 minutes (cleared on save); the `EmailSettings` section (Mailpit in docker compose) is only the fallback. The Hostinger settings were moved from `.env` into the database and removed from `.env`. Checked: 14 checks (encrypted column, no password in API/DB/audit/log, refusal on server change, real email from DB settings) + decryption after a container recreate. |
| Email sender name = company name | The "From" name of every email is now the company name from Project configuration, in the email's language (Arabic name for `ar`, English name otherwise) — `Application/Features/Settings/EmailSenderNameProvider`, used by `SmtpEmailService` and shown on Settings > Email. `EmailSettings:SenderName` (`SMTP_SENDER_NAME`) is only the fallback when no company name is saved; removed from `.env`. Checked with Mailpit: Arabic email from "أسم الشركة", English email from "System Test". |
| 2-hour session with renewal | The sign-in session now lasts 2 hours (`JWT:DurationInMinutes` = 120; docker compose `JWT_DURATION_MINUTES`). New `POST /api/Account/refresh-session` (signed in) extends the same session for another full lifetime and returns a new token (roles are read again); an ended, replaced or expired session, or an inactive user, cannot renew (`ApplicationUser.RenewSession`). UI: 5 minutes before the end a dialog with a countdown asks "renew?"; "Yes" renews and stays signed in, "Sign out" signs out, no answer signs out at the end with "session ended". Open tabs stay in step (renew / sign-out in one tab applies to all). Mailpit now accepts any SMTP login. Checked in headless Chrome with a 6-minute session (12/12) plus the time-out case. |
| Real email through Hostinger | SMTP settings moved into the Git-ignored `.env` (`SMTP_*`); docker compose gained `SMTP_SENDER_NAME` and `RESET_PASSWORD_URL`. Sent a real test email, an OTP and a login notice successfully through `smtp.hostinger.com:465`. |
| Added user groups and email testing | **Groups:** new `UserGroup` aggregate (name, description, roles) with tables `UserGroups`, `UserGroupRoles`, `UserGroupMembers` (migration `AddUserGroups`). A user can be in several groups; at login the JWT gets the user's own roles **plus** the roles of all their groups (`UserRoleResolver`), so every existing `RequireRole` works unchanged. Removing a role from a group, taking a user out of a group or deleting a group signs the affected members out; added roles apply at the next login. You can never take Administrator away from yourself through groups. API `/api/UserGroups` (list, details, create, update, delete, a user's groups, `setUserGroups`); `registerUserFull` accepts `groupIds`. UI: "User groups" page, a "Groups" button per user, groups in the new-user dialog. **Email:** Mailpit added to docker compose (SMTP `mailpit:1025`, inbox http://localhost:8025, nothing leaves the machine); real SMTP is set with `SMTP_*` in `.env`; new `EmailSettings:Security` (`Auto`/`SslOnConnect`/`StartTls`/`None`); new page Settings > Email shows the current settings and sends a test email with the real result (`GET email-status`, `POST test-email`, Administrator). Checked: 26 API checks + 15 browser checks. |
| Completed the Identity screens | UI: **Forgot password** (`/auth/forgot-password`, link on the login page), **Reset password** (`/auth/reset-password?email=&token=`, opened from the email; `EmailSettings:ResetPasswordUrl` now points there), **My account** (`/account`, in the side menu and the user menu: my details + change password, then signed out everywhere), **Roles** (`/roles`, for `Administrator`/`GetAllRolesList`: search, paging, create for `Administrator`/`CreateRole`). Side menu group "Users and permissions". Development: when SMTP is not set, `SmtpEmailService` saves each email as an `.html` file in `EmailSettings:PickupDirectory` (`emails/` in Development, mounted from `./emails` in docker compose, ignored by Git) so reset links and codes can be opened locally. Checked in headless Chrome: forgot -> email -> reset -> old password refused -> new password works -> change back from My account -> roles list/create/duplicate. |
| Added the Users module; stopped logging passwords | UI: new lazy `features/users` (side menu "Users", for `Administrator`/`GetAllUsers`): searchable, paged list; create user (with roles for Administrator, via `registerUserFull`; without roles via `register` for `RegisterUser`); edit details and active/inactive; roles dialog. API: new `PUT /api/UserRoles/setUserRoles` (Administrator) replaces a user's roles and invalidates their token when roles are removed; you cannot remove your own Administrator role or deactivate yourself (new messages `CannotRemoveOwnAdministrator`, `CannotDeactivateYourself`). **Security fix:** `LoggingBehavior` used to write every request, including passwords, into the logs; it now masks password/OTP/token/secret/stamp/key fields and shortens long values (`SafeLogData`). A request cancelled by the browser is no longer reported as a 500 error (`CustomExceptionHandler` returns 499 and logs it as information). Checked in headless Chrome (15/15). |
| Fixed saving on the configuration screen | The save API was working; the UI had two problems. (1) The only Save button was at the top of the page, so changes made further down (like the logo) were easy to lose: now a sticky "You have unsaved changes" bar with Save/Discard stays at the bottom while the form has changes, and leaving the page asks first (`unsavedChangesGuard` + `beforeunload`). (2) The success toast sat on top of that Save button and blocked the next click: toasts moved to the top corner under the nav bar. Checked in a real (headless Chrome) browser: login -> OTP -> edit -> save twice, plus an 18.8 MB logo resized to 155 KB and saved. |
| Logo upload accepts any image size | The configuration screen no longer refuses big files: `core/utils/image-resize.ts` resizes the image in the browser (longest side 512 px, saved as WEBP with transparency kept, quality lowered until it fits ~250 KB) before it is saved. Accepts any image the browser can open (PNG, JPEG, WEBP, GIF, SVG, BMP...); SVGs are turned into a picture, so no SVG code is stored. The API limit is unchanged. |
| Added the Angular UI and project configuration | New `UI/` (Angular 21): login, OTP verification, dashboard, side menu + nav bar, dark/light theme, Arabic (RTL) / English, project configuration screen, lazy-loaded features, JWT with interceptors and guards. Backend: `ProjectConfiguration` aggregate (company, currency, colors, fonts as value objects), `GET`/`PUT /api/Configuration`, migration `AddProjectConfiguration`, CORS for `http://localhost:4200` (`Cors:AllowedOrigins`). `.dockerignore` now skips `UI/`. |
| Created the `ServiceDb` database and the Administrator user | Recreated the `servicesa-sqlserver` container (port 1434). The database is now `ServiceDb` (docker compose and `appsettings.json`); all migrations were applied on startup. `AdminSeedOptions` gained `DateOfBirth` and `PhoneNumber`; the seeded user is `Administrator` (Omar Fathy, `omar-fathy@outlook.com`) with the Administrator role; the password is `ADMIN_PASSWORD` in `.env`. The old `ServicesA` database is still in the volume and was not deleted. |
| Added Swagger UI | `Swashbuckle.AspNetCore.SwaggerUI` 10.3.1 at `/swagger` (Development), reading the built-in document `/openapi/v1.json`. `OpenApi/BearerSecurityTransformers.cs` adds the JWT "Authorize" button and marks only protected endpoints with a lock; the token is kept across page reloads. |
| Added the Identity endpoints | 16 endpoints from AvocadoERP (login + OTP, sessions, users, roles, user roles) with JWT, MailKit email, rate limiting, paging (`BuildingBlocks.Pagination`) and `errorCode` in ProblemDetails. Regenerated `InitialCreate` (new user/role columns), applied at startup with role + admin seeding. SQL Server host port moved to 1434. Security holes of the original were fixed (see "Identity endpoints"). Tested end to end in docker compose. |
| Added BuildingBlocks (3 projects) | `BuildingBlocks`, `BuildingBlocks.Localization`, `BuildingBlocks.Messaging`, modeled on the AvocadoERP ones. Application uses the MediatR behaviors; API uses the exception handler and localization; Dockerfile copies `BuildingBlocks/`. Messaging is not wired yet (no RabbitMQ). |
| Added Carter, Mapster, MediatR, FluentValidation, EF Core | Each in its proper layer (see table above). |
| Added service registration | `GlobalUsing.cs` and `ServiceInjection.cs` in API, Application and Infrastructure; wired in `Program.cs`; added a DbContext and the connection string. |
| Moved `Users` to `Entities/Identity`, added all Identity table classes | Namespace is now `ServicesA.Domain.Entities.Identity`; `AppDbContext` uses the explicit user/role/claim/login/token classes; one configuration per table. |
| Fixed NuGet vulnerability warnings | Pinned `System.Security.Cryptography.Pkcs` 10.0.14 in Infrastructure (NU1902/NU1903). |
| Generated the `InitialCreate` migration | Added `AppDbContextFactory`; fixed invalid JSON (unescaped backslash) in `appsettings.json`. Not applied to the database yet. |
| Set the SQL Server `sa` password | Password moved to the Git-ignored `.env` (`MSSQL_SA_PASSWORD`); `sa` is enabled in the container. |
| Added SQL Server to docker compose | `sqlserver` service with volume and healthcheck; the API depends on it and uses it through an environment connection string. |
| Added Serilog | `Serilog.AspNetCore` in API; console + rolling daily file sink (`logs/servicesa-.log`, 14 days) configured in `appsettings.json`; Debug level in Development; request logging enabled; `logs/` ignored by Git. |
| Added Identity, history tables, DDD building blocks | `AppDbContext`, `IAppDbContext`, entity configuration files, `AuditLog`, `LoginHistory`, `ApplicationUser`/`ApplicationRole`. Replaced `ApplicationDbContext`. Migration not generated yet (blocked by Application Control). |

### Build and run
```bash
dotnet build SystemArchitecture.slnx
dotnet run --project Services/servicesA/ServicesA.API
docker compose up --build
```

### Identity, history tables and DDD
- **Domain** (no EF dependency):
  - `Common/`: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`, `IHasDomainEvents`, `IAuditableEntity`.
  - `Entities/Identity/`: `ApplicationUser` (aggregate root, Guid key, private setters, `Create` factory, raises `UserRegisteredDomainEvent`), `ApplicationRole`, `FullName` (value object), plus one class for each remaining Identity table: `ApplicationUserRole`, `ApplicationUserClaim`, `ApplicationUserLogin`, `ApplicationUserToken`, `ApplicationRoleClaim`.
  - `Auditing/`: `AuditLog` (history of data changes) and `LoginHistory` (history of login attempts).
- **Application**: `Abstractions/IAppDbContext` exposes `Users`, `Roles`, `AuditLogs`, `LoginHistories` and `SaveChangesAsync`.
- **Infrastructure**: `Persistence/AppDbContext` (`IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`) creates the standard Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`). On save it fills `CreatedAtUtc`/`ModifiedAtUtc` and writes an `AuditLog` row per changed entity (password hash and stamps are excluded). Entity configurations are in `Persistence/Configurations/` (one file per entity, applied with `ApplyConfigurationsFromAssembly`).
- Domain events are raised and stored on the aggregate but not dispatched yet.
- The `dotnet-ef` tool is pinned in [dotnet-tools.json](dotnet-tools.json). Migration command (do not apply to a database yet):
```bash
dotnet dotnet-ef migrations add InitialCreate --project Services/servicesA/ServicesA.Infrastructure --startup-project Services/servicesA/ServicesA.API --output-dir Persistence/Migrations
```
- **Status:** `InitialCreate` (in `Persistence/Migrations`) includes the Identity login columns and is applied automatically at startup when `Database:InitializeOnStartup` is true (on in Development and docker compose). `AppDbContextFactory` (design-time) lets `dotnet ef` work without building the API host. On this machine Windows Application Control blocks `dotnet ef` (error 0x800711C7), so the migration was generated inside the .NET SDK Docker image:
```bash
docker run --rm -v "<src>:/mnt/src:ro" -v "<src>/Services/servicesA/ServicesA.Infrastructure/Persistence/Migrations:/out" mcr.microsoft.com/dotnet/sdk:10.0 bash -c "mkdir /work && cd /mnt/src && tar --exclude=bin --exclude=obj -cf - . | (cd /work && tar xf -) && cd /work && dotnet tool restore && dotnet restore SystemArchitecture.slnx && dotnet dotnet-ef migrations add <Name> --project Services/servicesA/ServicesA.Infrastructure --startup-project Services/servicesA/ServicesA.API --output-dir Persistence/Migrations && cp Services/servicesA/ServicesA.Infrastructure/Persistence/Migrations/*.cs /out/"
```

### Identity endpoints
Try them in Swagger UI: http://localhost:5000/swagger (Development). Same routes as AvocadoERP, built with Carter → MediatR → Application handlers. Login is two steps: `login` checks the password and emails a 6-digit code, then `verify-otp` returns the JWT.

| Method | Route | Access |
|--------|-------|--------|
| POST | `/api/Account/login` | anonymous, rate limited |
| POST | `/api/Account/verify-otp` | anonymous, rate limited |
| POST | `/api/Account/resend-otp` | anonymous, rate limited (only after a password-checked login) |
| POST | `/api/Account/forgot-password` | anonymous, rate limited (always returns `true`) |
| POST | `/api/Account/reset-password` | anonymous, rate limited |
| POST | `/api/Account/logout` | signed in |
| POST | `/api/Account/refresh-session` | signed in (renews the current session, new token) |
| POST | `/api/Account/change-password` | signed in (user comes from the token) |
| POST | `/api/Account/register` | `RegisterUser` or `Administrator` |
| POST | `/api/Account/registerUserFull` | `Administrator` (user + roles) |
| POST | `/api/Account/editUser` | `EditUser` or `Administrator` |
| GET | `/api/Account/getAllUserList?pageNumber=&pageSize=&findValues=` | `GetAllUsers` or `Administrator` |
| GET | `/api/Account/getOTP?fullName=` | **Development only** (not mapped in other environments) |
| POST | `/api/Roles/createRole` | `CreateRole` or `Administrator` |
| GET | `/api/Roles/getAllRolesList` | `GetAllRolesList` or `Administrator` |
| POST | `/api/UserRoles/createUserRole` | `CreateUserRole` or `Administrator` |
| GET | `/api/UserRoles/getAllUserRolesList/{userId}` | `GetAllRolesList` or `Administrator` |
| PUT | `/api/UserRoles/setUserRoles` | `Administrator` (replaces the roles; not your own Administrator role) |
| GET | `/api/UserGroups/getAllGroups?pageNumber=&pageSize=&findValues=` | `GetAllRolesList` or `Administrator` |
| GET | `/api/UserGroups/{id}` (roles + members) | `GetAllRolesList` or `Administrator` |
| GET | `/api/UserGroups/user/{userId}` | `GetAllRolesList` or `Administrator` |
| POST | `/api/UserGroups/createGroup` | `Administrator` |
| PUT | `/api/UserGroups/updateGroup` | `Administrator` |
| DELETE | `/api/UserGroups/{id}` | `Administrator` |
| PUT | `/api/UserGroups/setUserGroups` | `Administrator` |
| GET | `/api/Configuration/email-settings` | `Administrator` (no password, only `hasPassword`) |
| PUT | `/api/Configuration/email-settings` | `Administrator` (password encrypted before saving) |
| POST | `/api/Configuration/test-email` | `Administrator`, rate limited |

**Email server:** saved in the database from Settings > Email (password encrypted). Until something is saved there, the fallback below is used. **Email in development:** docker compose starts **Mailpit**; every email (OTP codes, reset links, test emails) lands in its inbox at http://localhost:8025 and nothing is really sent. To use a real server put these in `.env` and run `docker compose up -d`: `SMTP_SERVER`, `SMTP_PORT`, `SMTP_SECURITY` (`Auto`, `StartTls`, `SslOnConnect` or `None`), `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_SENDER` (the sender name is the company name from Project configuration; `SMTP_SENDER_NAME` is only a fallback) (and optionally `RESET_PASSWORD_URL`, default `http://localhost:4200/auth/reset-password`). Then check it from Settings > Email > "Send a test email". The `.env` file is ignored by Git, so the SMTP password is never committed; it is currently set for Hostinger (`smtp.hostinger.com:465`, SSL). Running the API without docker (`dotnet run`) keeps the `emails/` folder fallback.

How it works:
- **Domain**: `ApplicationUser` owns the rules: `IssueOtp` / `TryConsumeOtp` (5-minute code, max 5 wrong tries, constant-time compare), `StartSession` / `EndSession` / `IsSessionValid` (one active session), `UpdateProfile`, `Activate` / `Deactivate`. New columns: `DateOfBirth`, `LastActiveAtUtc`, `OtpCode`, `OtpExpiresAtUtc`, `OtpFailedAttempts`, `ActiveSessionId`, `ActiveSessionExpiresAtUtc`; `AspNetRoles.Title`. Role names are in `IdentityRoles`.
- **JWT** (`Infrastructure/Identity/JwtTokenService`): claims `sub`, `name`, `email`, `role`, `sid` (session) and `security_stamp`. On every request `OnTokenValidated` checks that the session is still the user's active one and the security stamp is unchanged, so logout, a forced new login, password change/reset and deactivation take effect immediately. Settings: `JWT` section (`Key` ≥ 32 bytes, checked at startup; a dev-only key is in `appsettings.Development.json`, production must set `JWT__Key`).
- **Single session**: a second login while a session is active returns 400 with `errorCode: "USER_ALREADY_LOGGED_IN"`; send `"forceLogin": true` to take over.
- **Lockout**: 5 wrong passwords lock the account for 5 minutes. Password policy: 8+ chars with upper, lower, digit and symbol.
- **Email** (`Infrastructure/Email/SmtpEmailService`, MailKit): `EmailSettings` section. When `SmtpServer` is empty, emails are only logged, and you read the code from `getOTP` in Development. Passwords are never emailed.
- **History**: every password/OTP attempt adds a `LoginHistories` row (IP, user agent, success). `AuditLogs` skip `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `OtpCode` and `ActiveSessionId`.
- **Startup** (`DatabaseInitializer`): applies migrations, creates the roles in `IdentityRoles`, and creates the first Administrator from `Seed:Admin` when `Seed:Admin:Password` is set (docker compose takes it from `ADMIN_PASSWORD` in `.env`; the user is `Administrator` / `omar-fathy@outlook.com`, with name, birth date and phone from `Seed__Admin__*` in `docker-compose.yml`). The database is `ServiceDb`.
- **Messages** follow `Accept-Language` (`en`/`ar`), including FluentValidation messages (`UseRequestLocalization`). Errors are ProblemDetails; bad JSON or query values return 400.

Changes from the AvocadoERP version (security):
- `getOTP` was public and returned anyone's code; now Development only.
- `register`, `editUser`, `createRole`, `createUserRole` had their `[Authorize]` commented out, so anyone could create users or make themselves Administrator; they now require roles.
- The password is no longer emailed (the original also sent a copy to a hard-coded address).
- Logout, password change/reset and deactivation now really invalidate tokens; the original only cleared a field, and its tokens stayed valid.
- OTPs use `RandomNumberGenerator` instead of `Random`, and `verify-otp` / `reset-password` are rate limited.
- Unknown user names and emails get the same answer as wrong ones.
- SMTP certificate validation stays on, and the JWT issuer/audience come from config instead of hard-coded `"SecureApi"`.
- Branch fields (`BranchIds`) and photos were left out because this project has no branches or photos.
- The request body uses `firstName`/`lastName` (the `FullName` value object) instead of a single `fullName`, and the language comes from the `Accept-Language` header instead of a `currentLanguage` field.

### BuildingBlocks
Shared projects under `BuildingBlocks/`, referenced by the services.

| Project | Contents | Used by |
|---------|----------|---------|
| `BuildingBlocks` (MediatR 15, FluentValidation 12.2) | `CQRS/`: `ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`. `Behaviors/`: `LoggingBehavior` (logs every request, warns above 3 s), `ValidationBehavior` (runs validators for commands, throws `ValidationException`), `Sorting`. `Exceptions/`: `BadRequestException`, `NotFoundException`, `InternalServerException`, `Handler/CustomExceptionHandler` (returns ProblemDetails with status 400/404/500, `traceId` and `ValidationErrors`). `Options/JwtOptions` (`JWT` section). `DependencyInjection`: `AddBuildingBlocksBehaviors()` and `AddCustomExceptionHandler()`. | Application (behaviors), API (exception handler, `app.UseExceptionHandler()`) |
| `BuildingBlocks.Localization` | `ILanguageService` (reads `Accept-Language`, supports `en`/`ar`, defaults to `en`), `IStringLocalizerService.Get(key)`, `ResourceKeys`, `Resources/SharedResources.en.resx` and `.ar.resx`. Registered with `AddLocalizationDependencyInjection()`. | API |
| `BuildingBlocks.Messaging` (MassTransit.RabbitMQ 8.5.12) | `IntegrationEvent` base record, `AddMessageBroker(configuration, assembly)` reading `MessageBroker:Host`, `UserName`, `Password`. | not wired yet |

### Project configuration (backend)
One row of app-wide settings used by the UI, stored in the `ProjectConfigurations` table.

| Method | Route | Access |
|--------|-------|--------|
| GET | `/api/Configuration` | anonymous (the login page needs the company name, colors and fonts); returns the defaults until something is saved |
| PUT | `/api/Configuration` | `Administrator` |

- **Domain** (`Entities/Settings/`): `ProjectConfiguration` (aggregate root, auditable) with value objects `CompanyDetails` (names en/ar, email, phone, address, website, tax number, logo), `CurrencySettings` (ISO code, symbol, 0-4 decimals), `ThemeColors` (`#RRGGBB` main color, light and dark backgrounds), `Typography` (English/Arabic Google Fonts names, base size 12-20 px), plus `DefaultLanguage` (`en`/`ar`) and `DefaultTheme` (`light`/`dark`). Mapped with EF complex properties.
- **Validation**: font names are letters/digits/spaces only and colors must be hex, so they are safe to put in CSS; the logo must be an http(s) URL or a PNG/JPEG/WEBP/GIF data URL up to about 200 KB (SVG is refused); the website must be http(s).
- **CORS**: `Cors:AllowedOrigins` in `appsettings.json` (default `http://localhost:4200`); the `Pagination` and `Retry-After` headers are exposed.

### Angular UI
Angular 21 (standalone components, signals, zoneless) in `UI/`. Talks to the API at `http://localhost:5000` (`src/environments/`).

```bash
cd UI
npm install
npm start        # http://localhost:4200 (API must be running: docker compose up -d)
npm run build    # production build in UI/dist/
```

| Requirement | Where |
|-------------|-------|
| Login | `features/auth/login`: user name + password -> `POST /api/Account/login`. If the account is open elsewhere (`USER_ALREADY_LOGGED_IN`) it offers "sign in here and end the other session" (`forceLogin`). |
| Verify OTP | `features/auth/verify-otp`: 6-digit code -> `POST /api/Account/verify-otp`, submits by itself on the 6th digit, accepts pasted codes, resend with a 30 s countdown. Needs step 1 first (`otpGuard`). In Development read the code from `GET /api/Account/getOTP?fullName=` in Swagger, because no SMTP server is set. |
| Dashboard | `features/dashboard`: counts of users and roles (only called when the user has the role), my roles, session end time, profile, company + currency format, users table. |
| Side menu and nav bar | `layout/`: collapsible side menu (remembered), slides over the page on phones; items are filtered by role. Nav bar: menu button, language, theme, user menu with logout. |
| Dark and light theme | `ThemeService`: `data-theme` on `<html>`, CSS variables in `src/styles/_tokens.scss`; remembered per browser, otherwise the project default. |
| Arabic (RTL) and English | `LanguageService` sets `lang`/`dir` on `<html>`; texts in `core/i18n/translations.ts` with the `t` pipe; all CSS uses logical properties so the layout flips; `Accept-Language` is sent so API messages come back in the same language. |
| Project configuration | `features/settings/project-configuration` (Administrator only): company details + logo upload (any size, resized in the browser), currency, colors (picker + hex), fonts, default language/theme, with a live preview. `AppConfigService` loads it before the app starts (`provideAppInitializer`) and applies colors/fonts as CSS variables and Google Fonts. |
| Forgot / reset password | `features/auth/forgot-password` and `reset-password`. The forgot page always shows the same message (no way to find out which emails exist). In Development open the newest file in `emails/` and click the button. |
| My account | `features/account`: my details and change password (current + new + confirm). The API ends every session after a change, so the UI signs out and asks to sign in with the new password. |
| User groups | `features/groups` (view: Administrator or `GetAllRolesList`; change: Administrator): list with roles and member count, create/edit (name, description, roles; members shown), delete with confirmation. A user's groups are set from the Users page ("Groups" button) or when creating the user. |
| Email | `features/settings/email-settings` (Administrator): current mode (SMTP / files / off), server, security, sender, a link to the Mailpit inbox, and "Send a test email" with the real result or SMTP error. |
| Roles | `features/roles` (Administrator or `GetAllRolesList`): list with search and paging; "Add role" for Administrator or `CreateRole` (code name + title + description). |
| Users | `features/users` (Administrator or `GetAllUsers`): list with search and paging, create (password checked against the policy, roles for Administrator), edit (details, active), roles dialog (`setUserRoles`). Buttons appear only for the roles the API accepts. Your own account cannot lose Administrator or be deactivated. |
| Lazy loading | `app.routes.ts`: `auth`, `dashboard`, `users`, `roles`, `account` and `settings` are separate chunks loaded with `loadChildren`; `canMatch` guards run before a chunk is downloaded. |
| JWT, interceptors, guards | `AuthService` keeps the token (localStorage) and its expiry from the `exp` claim; 5 minutes before the end `layout/session-expiry` asks to renew (`refresh-session`), otherwise it signs out when it expires. Interceptors: `languageInterceptor` (Accept-Language), `authInterceptor` (Bearer token, only for the API URL), `errorInterceptor` (401 -> sign out, 403/5xx/network -> message). Guards: `authGuard`, `guestGuard`, `otpGuard`, `roleGuard(...)`. |

The password is kept in memory only between login and verify-otp, because the API's `verify-otp` checks it again. A redirect after login (`returnUrl`) only accepts paths inside the app.

Differences from the AvocadoERP version: `LoggingBehavior` measures `TotalSeconds` (the original used `Seconds`, which ignores minutes); `IntegrationEvent.Id`/`OccurredOnUtc` are set once (the original created a new value on every read); localization reads the right resource name and handles headers like `ar-EG,ar;q=0.9`; `JWT` became `Options/JwtOptions`; the project-specific `BasketCheckoutEvent` and unused packages (Newtonsoft.Json, FeatureManagement, Mapster, EF Core) were left out; MassTransit is 8.x because 9.x needs a commercial license.

---

## العربية

### نظرة عامة
سوليشن بـ .NET 10 بيحتوي على مايكروسيرفسز. كل سيرفس بتتبع **Clean Architecture** من 4 طبقات. حاليًا فيه سيرفس واحدة هي `servicesA`.

### هيكل الفولدرات

```
src/
├── SystemArchitecture.slnx     # ملف السوليشن
├── docker-compose.yml          # تشغيل السيرفسز في كونتينرز
├── Directory.Build.props       # إعدادات مشتركة: net10.0 و Nullable و ImplicitUsings
├── global.json                 # تثبيت إصدار الـ SDK
├── dotnet-tools.json           # أدوات محلية (dotnet-ef 10.0.14)
├── .gitignore / .dockerignore  # ملفات التجاهل
├── BuildingBlocks/             # كود مشترك بين السيرفسز
│   ├── BuildingBlocks/               # CQRS و MediatR behaviors و الـ exceptions والـ handler و options
│   ├── BuildingBlocks.Localization/  # ترجمة حسب Accept-Language (ملفات resx للإنجليزي والعربي)
│   └── BuildingBlocks.Messaging/     # MassTransit + RabbitMQ و integration events
├── UI/                         # واجهة Angular 21 (شوف "واجهة Angular")
└── Services/
    └── servicesA/
        ├── ServicesA.API                 # Program.cs, GlobalUsing.cs, ServiceInjection.cs (AddApi)
        ├── ServicesA.Application         # GlobalUsing.cs, ServiceInjection.cs (AddApplication)
        │   └── Abstractions/             # IAppDbContext
        ├── ServicesA.Domain
        │   ├── Common/                   # Entity, AggregateRoot, ValueObject, domain events, IAuditableEntity
        │   ├── Auditing/                 # AuditLog, LoginHistory, AuditAction
        │   └── Entities/Identity/        # ApplicationUser/Role/UserRole/UserClaim/UserLogin/UserToken/RoleClaim, FullName, Events/
        └── ServicesA.Infrastructure      # GlobalUsing.cs, ServiceInjection.cs (AddInfrastructure)
            └── Persistence/              # AppDbContext, Configurations/ (one per entity), Migrations/ (pending)
```

### الملفات الأساسية
| الملف | الوظيفة |
|------|---------|
| `SystemArchitecture.slnx` | السوليشن، بيجمع كل المشاريع. |
| `docker-compose.yml` | بيبني ويشغل `servicesa-api` (بورت 5000 على الجهاز -> 8080 في الكونتينر) و `sqlserver` (SQL Server 2025 على بورت 1434 على الجهاز -> 1433 في الكونتينر، والداتا في volume اسمه `sqlserver-data`). الـ API بيستنى الـ healthcheck بتاع SQL Server وبياخد الـ connection string من `ConnectionStrings__DefaultConnection`. باسورد `sa` من `MSSQL_SA_PASSWORD` (متحدد في ملف `.env` المتجاهل من Git؛ وبيرجع لقيمة افتراضية للتطوير بس لو `.env` مش موجود). ويوزر `sa` مفعّل افتراضيًا في الكونتينر. |
| `Directory.Build.props` | بيطبق `net10.0` و nullable و implicit usings على كل المشاريع. |
| `global.json` | بيثبت الـ SDK على 10.0.x (مع roll forward لأحدث إصدار). |
| `dotnet-tools.json` | مانيفست الأدوات اللي بيثبت `dotnet-ef` 10.0.14 (بيتشغل بـ `dotnet dotnet-ef`). |
| `.gitignore` | بيستثني `bin/` و `obj/` و `.vs/` و `.idea/` و `*.user` من Git. |
| `.dockerignore` | بيستثني `bin` و `obj` و `UI/` و `node_modules` و `.vs` و `.git` و `logs` من سياق بناء Docker. |

### طبقات Clean Architecture (servicesA)
| الطبقة | المسؤولية | بتشاور على |
|-------|-----------|------------|
| **Domain** | الـ Entities والـ Value Objects وقواعد الدومين. من غير أي اعتماديات. | لا شيء |
| **Application** | الـ Use Cases والـ Interfaces والـ DTOs ومنطق العمل. | Domain |
| **Infrastructure** | تنفيذ الأمور الخارجية: قاعدة البيانات، الرسائل، الـ APIs الخارجية. | Application |
| **API** | نقطة الدخول (ASP.NET Core Web API)، الـ endpoints وتجميع الـ DI. | Application و Infrastructure |

الاعتماديات بتتجه للداخل: API -> Infrastructure -> Application -> Domain.

### مشروع الـ API
- بيستخدم **top-level statements** ([Program.cs](Services/servicesA/ServicesA.API/Program.cs))، من غير `Main` ولا كلاس `Program`.
- بيفعّل OpenAPI في بيئة Development.
- فيه endpoint بسيط `GET /` بيرجع `ServicesA is running`.
- فيه `Dockerfile` بيبني بصورة .NET 10 SDK ويشغل على صورة ASP.NET 10 runtime.
- بيسجّل كل حاجة في [Program.cs](Services/servicesA/ServicesA.API/Program.cs) بـ `AddApplication()` و `AddInfrastructure(configuration)` و `AddApi()`، وبعدها بينادي `app.MapCarter()`.

### الحزم ومكانها
| الطبقة | الحزمة | الإصدار | بتتسجل في |
|-------|--------|---------|-----------|
| API | Carter | 10.0.0 | `AddApi()` -> `AddCarter()` |
| API | Serilog.AspNetCore | 10.0.0 | `AddSerilogLogging()` -> `UseSerilog(...)` من قسم `Serilog`؛ و `UseSerilogRequestLogging()` |
| API | Microsoft.EntityFrameworkCore.Design | 10.0.14 | وقت التصميم بس (الـ migrations) |
| Application | MediatR | 15.0.0 | `AddApplication()` -> `AddMediatR(...)` |
| Application | FluentValidation.DependencyInjectionExtensions | 12.2.0 | `AddApplication()` -> `AddValidatorsFromAssembly(...)` |
| Application | Mapster + Mapster.DependencyInjection | 10.0.14 | `AddApplication()` -> بيعمل scan للـ mappings ويسجل `IMapper` |
| Infrastructure | Microsoft.EntityFrameworkCore.SqlServer | 10.0.14 | `AddInfrastructure()` -> `AddDbContext<AppDbContext>` |
| Infrastructure | System.Security.Cryptography.Pkcs | 10.0.14 | مثبتة مباشرة لتعويض النسخة 9.0.11 المصابة اللي بتيجي من `Microsoft.Data.SqlClient` (حل NU1902/NU1903) |
| Infrastructure | Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.14 | `AddInfrastructure()` -> `AddIdentityCore<ApplicationUser>()...AddEntityFrameworkStores<AppDbContext>()` |
| Application | Microsoft.EntityFrameworkCore | 10.0.14 | أنواع `DbSet` في `IAppDbContext` |
| Domain | Microsoft.Extensions.Identity.Stores | 10.0.14 | الأنواع الأساسية `IdentityUser<Guid>` / `IdentityRole<Guid>` (من غير اعتماد على EF) |

Carter و EF Core مثبتين على إصدارات متوافقة مع net10 لأن أحدث إصداراتهم (11.x) بتدعم net11 بس.

كل طبقة من API و Application و Infrastructure فيها ملفين:
- `GlobalUsing.cs`: الـ global usings الخاصة بالطبقة.
- `ServiceInjection.cs`: الـ extension method `Add...` اللي بتسجل خدمات الطبقة.

الـ connection string باسم `DefaultConnection` موجود في [appsettings.json](Services/servicesA/ServicesA.API/appsettings.json) (SQL Server LocalDB افتراضيًا). و `AppDbContext` في `ServicesA.Infrastructure/Persistence`.

### سجل التعديلات
| التعديل | التفاصيل |
|---------|----------|
| قاموس الرسايل باللهجات + Toastr | **القاموس:** جدول جديد `LocalizedTexts` (migration اسمها `AddMessageDictionary`) فيه كل رسايل الباك إند (رسايل الـ API والإيميلات، 42 مفتاح) بالإنجليزي والمصري (`ar-EG`) والسعودي (`ar-SA`)، وبيتملى عند التشغيل للناقص بس (`Persistence/Seed/LocalizedTextSeed`)، فالتعديلات بتفضل. حقل جديد للشركة **الدولة** (`EG` / `SA` / فاضي = فصحى) في إعدادات المشروع بيحدد اللهجة العربي. `BuildingBlocks.Localization` بقى فيه `ILocalizedTextStore`: `StringLocalizerService` بيقرا لهجة الشركة، وبعدها اللغة، وبعدها ملفات الـ resx. `DbLocalizedTextStore` بيحفظ القاموس كله في الذاكرة وبيعيد تحميله بعد الحفظ. صفحة جديدة الإعدادات > قاموس الرسايل (`GET`/`PUT /api/Configuration/messages`، للـ Administrator). **Toastr:** `ngx-toastr` 20.0.5؛ و `ToastService` بقى بيعرض إشعارات Toastr (بألوان السيستم، في الركن اللي فوق، ومقلوبة للعربي)؛ والرسايل الثابتة في صفحات الدخول والكود ونسيت/تغيير الباسورد والـ dialogs وحسابي والإيميل بقت toasts، و"الحساب مفتوح على جهاز تاني" بقت dialog تأكيد. **إصلاح الـ audit:** `AuditLogs` بقت بتسجل كمان حقول الـ value objects (زي `Company.NameAr` و `Currency.Symbol`)؛ والقيم الطويلة زي اللوجو بتتقصّر. اتجرّب: تغيير اللهجة (فصحى / مصري / سعودي / إنجليزي، في الرسايل وعناوين الإيميلات)، وتعديل نص بيتطبق فورًا، و 10 اختبارات في المتصفح للـ toasts وصفحة القاموس. |
| تصميم جديد للإيميلات من إعدادات الشركة | كل إيميلات السيستم (OTP، وإشعار الدخول، والترحيب، وتغيير الباسورد، وتأكيد التغيير، والتجريبي) بقت بتستخدم قالب واحد جديد (`Application/Features/Identity/Common/EmailTemplates`، وبقى service) مبني من إعدادات المشروع في قاعدة البيانات عن طريق `EmailBrandingProvider` (cache لمدة 10 دقايق، وبيتمسح لما الإعدادات تتحفظ): اللون الأساسي (الشريط اللي فوق، وصندوق الكود، والزرار، واللينكات)، والخلفية الفاتحة، والخط العربي/الإنجليزي، واللوجو (بيتبعت جوه الإيميل كـ `cid:company-logo` لأن Gmail و Outlook بيمنعوا صور `data:`)، واسم الشركة بلغة الإيميل، والعنوان/التليفون/الإيميل/الموقع في الفوتر لو متكتبين. الأكواد بتظهر كبيرة ومتباعدة؛ والزراير شغالة في Outlook وتحتها اللينك نفسه؛ واتضاف نسخة نص عادي؛ والتصميم بيتقلب للعربي. واللوجو بقى بيتحفظ PNG لو ينفع (Outlook على ويندوز مش بيعرض WEBP). اتجرّب على Mailpit: إيميل تجريبي عربي وإنجليزي، وإيميل OTP، وإيميل تغيير الباسورد. |
| إعدادات سيرفر الإيميل في قاعدة البيانات | جدول جديد `EmailServerSettings` (migration اسمها `AddEmailServerSettings`)، وبيتعدل من الإعدادات > الإيميل (`GET`/`PUT /api/Configuration/email-settings`، للـ Administrator). باسورد الـ SMTP بيتشفر بـ ASP.NET Data Protection (`ISecretProtector`) قبل ما يتحفظ؛ ومفاتيح التشفير برا قاعدة البيانات في docker volume اسمه `dataprotection-keys` (`DataProtection:KeysPath`)، فنسخة من قاعدة البيانات لوحدها مش بتكشفه — وده كمان بيخلي لينكات تغيير الباسورد تفضل شغالة بعد إعادة التشغيل. الـ API عمره ما بيرجّع الباسورد (بس `hasPassword`)؛ والفاضي معناه سيبه زي ما هو؛ ولو غيرت السيرفر أو اسم المستخدم لازم تكتبه تاني (`SMTP_PASSWORD_REQUIRED`)؛ ومش بيتسجل في AuditLogs ولا اللوج. `IEmailSettingsProvider` بيقرا من قاعدة البيانات الأول وبيعمل cache لمدة 10 دقايق (بيتمسح مع الحفظ)؛ وقسم `EmailSettings` (Mailpit في docker compose) بقى احتياطي بس. إعدادات Hostinger اتنقلت من `.env` لقاعدة البيانات واتمسحت من `.env`. اتجرّب: 14 اختبار (العمود متشفر، والباسورد مش في الـ API ولا الداتابيز ولا الـ audit ولا اللوج، والرفض عند تغيير السيرفر، وإيميل حقيقي من إعدادات الداتابيز) وكمان فك التشفير بعد إعادة إنشاء الـ container. |
| اسم مرسل الإيميل = اسم الشركة | اسم المرسل في كل إيميل بقى اسم الشركة من إعدادات المشروع، بلغة الإيميل (الاسم العربي لـ `ar`، والإنجليزي لغير كده) — `Application/Features/Settings/EmailSenderNameProvider`، وبيستخدمه `SmtpEmailService` وبيظهر في الإعدادات > الإيميل. و `EmailSettings:SenderName` (`SMTP_SENDER_NAME`) بقى احتياطي بس لو مفيش اسم شركة متحفظ؛ واتشال من `.env`. اتجرّب على Mailpit: الإيميل العربي من "أسم الشركة"، والإنجليزي من "System Test". |
| جلسة ساعتين مع التجديد | جلسة تسجيل الدخول بقت ساعتين (`JWT:DurationInMinutes` = 120؛ وفي docker compose `JWT_DURATION_MINUTES`). endpoint جديد `POST /api/Account/refresh-session` (لازم تكون مسجل دخول) بيمد نفس الجلسة مدة كاملة تانية وبيرجّع توكن جديد (والصلاحيات بتتقري من جديد)؛ والجلسة اللي انتهت أو اتبدلت أو خلص وقتها، أو المستخدم الموقوف، مينفعش تتجدد (`ApplicationUser.RenewSession`). الواجهة: قبل النهاية بـ 5 دقايق بتظهر رسالة فيها عداد بتسأل "تجدد؟"؛ "أيوه" بتجدد وتفضل مسجل دخول، و"تسجيل الخروج" بتخرّجك، ولو مردتش بيحصل تسجيل خروج عند النهاية مع رسالة "الجلسة انتهت". والتابات المفتوحة بتمشي مع بعض (التجديد أو الخروج في تاب بيتطبق على الكل). و Mailpit بقى بيقبل أي دخول SMTP. اتجرّب في Chrome headless بجلسة 6 دقايق (12/12) وكمان حالة انتهاء الوقت. |
| إيميل حقيقي عن طريق Hostinger | إعدادات الـ SMTP اتنقلت لملف `.env` المتجاهل في Git (`SMTP_*`)؛ و docker compose بقى فيه `SMTP_SENDER_NAME` و `RESET_PASSWORD_URL`. اتبعت إيميل تجريبي وكود OTP وإشعار دخول بنجاح عن طريق `smtp.hostinger.com:465`. |
| إضافة مجموعات المستخدمين وتجربة الإيميل | **المجموعات:** aggregate جديد `UserGroup` (اسم ووصف وصلاحيات) وجداول `UserGroups` و `UserGroupRoles` و `UserGroupMembers` (migration اسمها `AddUserGroups`). المستخدم ممكن يبقى في أكتر من مجموعة؛ وعند الدخول الـ JWT بياخد صلاحيات المستخدم نفسه **وكمان** صلاحيات كل مجموعاته (`UserRoleResolver`)، فكل `RequireRole` موجود بيشتغل من غير تغيير. لو شلت صلاحية من مجموعة أو طلّعت مستخدم من مجموعة أو مسحت مجموعة، الأعضاء المتأثرين بيتعملهم تسجيل خروج؛ والصلاحيات اللي بتتضاف بتشتغل من أول دخول جاي. ومينفعش أبدًا تشيل Administrator من نفسك عن طريق المجموعات. الـ API في `/api/UserGroups` (القائمة والتفاصيل والإضافة والتعديل والمسح ومجموعات مستخدم و `setUserGroups`)؛ و `registerUserFull` بقى بيقبل `groupIds`. الواجهة: صفحة "مجموعات المستخدمين"، وزرار "المجموعات" لكل مستخدم، والمجموعات في شاشة المستخدم الجديد. **الإيميل:** اتضاف Mailpit في docker compose (SMTP على `mailpit:1025`، والـ inbox على http://localhost:8025، ومفيش حاجة بتخرج من الجهاز)؛ والـ SMTP الحقيقي بيتحدد بـ `SMTP_*` في `.env`؛ وإعداد جديد `EmailSettings:Security` (`Auto`/`SslOnConnect`/`StartTls`/`None`)؛ وصفحة جديدة الإعدادات > الإيميل بتعرض الإعدادات الحالية وبتبعت إيميل تجريبي وبتقولك النتيجة الحقيقية (`GET email-status` و `POST test-email`، للـ Administrator). اتجرّب: 26 اختبار API و 15 اختبار في المتصفح. |
| استكمال شاشات Identity | الواجهة: **نسيت الباسورد** (`/auth/forgot-password`، واللينك في صفحة الدخول)، و**تغيير الباسورد من الإيميل** (`/auth/reset-password?email=&token=`، بيتفتح من الإيميل؛ و `EmailSettings:ResetPasswordUrl` بقى بيشاور عليها)، و**حسابي** (`/account`، في القائمة الجانبية وقائمة اليوزر: بياناتي وتغيير الباسورد، وبعدها تسجيل خروج من كل الأجهزة)، و**الصلاحيات** (`/roles`، لـ `Administrator`/`GetAllRolesList`: بحث وتقسيم صفحات وإضافة لـ `Administrator`/`CreateRole`). مجموعة جديدة في القائمة "المستخدمين والصلاحيات". في Development: لو مفيش SMTP، `SmtpEmailService` بيحفظ كل إيميل كملف `.html` في `EmailSettings:PickupDirectory` (`emails/` في Development، ومربوط بـ `./emails` في docker compose، ومتجاهل في Git) عشان تفتح لينكات تغيير الباسورد والأكواد على جهازك. اتجرّب في Chrome headless: نسيت الباسورد -> الإيميل -> تغيير الباسورد -> الباسورد القديم اترفض -> الجديد شغال -> رجّعته من حسابي -> قائمة الصلاحيات والإضافة والتكرار. |
| إضافة موديول المستخدمين، ووقف تسجيل الباسوردات في اللوج | الواجهة: `features/users` جديد بـ lazy loading (في القائمة "المستخدمين"، لـ `Administrator`/`GetAllUsers`): قائمة فيها بحث وتقسيم صفحات؛ وإضافة مستخدم (بصلاحيات للـ Administrator عن طريق `registerUserFull`، ومن غير صلاحيات عن طريق `register` لـ `RegisterUser`)؛ وتعديل البيانات والتفعيل/الإيقاف؛ وشاشة صلاحيات. الـ API: endpoint جديد `PUT /api/UserRoles/setUserRoles` (للـ Administrator) بيبدّل صلاحيات المستخدم وبيلغي التوكن بتاعه لو اتشالت صلاحيات؛ ومينفعش تشيل صلاحية Administrator من نفسك ولا توقف حسابك (رسايل جديدة `CannotRemoveOwnAdministrator` و `CannotDeactivateYourself`). **إصلاح أمني:** `LoggingBehavior` كان بيكتب كل request في اللوج ومعاه الباسوردات؛ دلوقتي بيخفي حقول password/OTP/token/secret/stamp/key وبيقصّر القيم الطويلة (`SafeLogData`). والـ request اللي المتصفح بيلغيه مبقاش بيظهر كخطأ 500 (`CustomExceptionHandler` بيرجّع 499 وبيسجله كمعلومة). اتجرّب في Chrome headless (15/15). |
| إصلاح الحفظ في شاشة الإعدادات | الـ API بتاع الحفظ كان شغال؛ المشكلة كانت في الواجهة في حاجتين. (1) زرار الحفظ الوحيد كان فوق في أول الصفحة، فالتعديلات اللي تحت (زي اللوجو) كانت بتضيع بسهولة: دلوقتي فيه شريط ثابت تحت "فيه تعديلات لسه متحفظتش" فيه حفظ وتجاهل طول ما فيه تعديلات، ولو خرجت من الصفحة بيسألك الأول (`unsavedChangesGuard` و `beforeunload`). (2) رسالة "اتحفظت" كانت بتغطي زرار الحفظ ده وبتمنع الضغطة اللي بعدها: الرسائل اتنقلت للركن اللي فوق تحت الـ Nav Bar. اتجرّب في متصفح حقيقي (Chrome headless): دخول -> OTP -> تعديل -> حفظ مرتين، وكمان لوجو 18.8 ميجا اتصغر لـ 155 كيلوبايت واتحفظ. |
| رفع اللوجو بأي حجم | شاشة الإعدادات مبقتش بترفض الملفات الكبيرة: `core/utils/image-resize.ts` بيصغّر الصورة في المتصفح (أطول ضلع 512 px، وبتتحفظ WEBP ومن غير ما الشفافية تضيع، والجودة بتقل لحد ما تبقى حوالي 250 كيلوبايت) قبل الحفظ. بيقبل أي صورة المتصفح يقدر يفتحها (PNG و JPEG و WEBP و GIF و SVG و BMP...)؛ والـ SVG بيتحوّل لصورة عادية فمفيش كود SVG بيتخزن. حد الـ API زي ما هو. |
| إضافة واجهة Angular وإعدادات المشروع | فولدر جديد `UI/` (Angular 21): تسجيل الدخول، والتحقق بالـ OTP، ولوحة التحكم، والقائمة الجانبية والـ Nav Bar، والثيم الفاتح/الداكن، والعربي (RTL) والإنجليزي، وشاشة إعدادات المشروع، وتحميل الـ features بـ lazy loading، و JWT مع interceptors و guards. في الباك إند: `ProjectConfiguration` كـ aggregate (الشركة والعملة والألوان والخطوط كـ value objects)، و `GET`/`PUT /api/Configuration`، و migration اسمها `AddProjectConfiguration`، و CORS لـ `http://localhost:4200` (`Cors:AllowedOrigins`). و `.dockerignore` بقى بيتجاهل `UI/`. |
| إنشاء قاعدة `ServiceDb` ويوزر Administrator | إعادة إنشاء container `servicesa-sqlserver` (بورت 1434). قاعدة البيانات بقت `ServiceDb` (في docker compose و `appsettings.json`)؛ وكل الـ migrations اتطبقت عند التشغيل. اتضاف `DateOfBirth` و `PhoneNumber` لـ `AdminSeedOptions`؛ واليوزر اللي اتعمل `Administrator` (Omar Fathy، `omar-fathy@outlook.com`) وليه صلاحية Administrator؛ والباسورد هو `ADMIN_PASSWORD` في `.env`. قاعدة `ServicesA` القديمة لسه موجودة في الـ volume ومتمسحتش. |
| إضافة Swagger UI | `Swashbuckle.AspNetCore.SwaggerUI` 10.3.1 على `/swagger` (في Development)، وبيقرأ الـ document الجاهز `/openapi/v1.json`. ملف `OpenApi/BearerSecurityTransformers.cs` بيضيف زرار "Authorize" للـ JWT وبيحط قفل على الـ endpoints المحمية بس؛ والتوكن بيفضل محفوظ لو عملت refresh للصفحة. |
| إضافة Identity Endpoints | 16 endpoint من AvocadoERP (login + OTP، والجلسات، واليوزرز، والصلاحيات، وصلاحيات اليوزر) بـ JWT وإيميل MailKit و rate limiting وتقسيم صفحات (`BuildingBlocks.Pagination`) و `errorCode` في الـ ProblemDetails. إعادة إنشاء `InitialCreate` (أعمدة جديدة لليوزر والصلاحية) وبتتطبق عند التشغيل مع إضافة الصلاحيات والأدمن. بورت SQL Server على الجهاز بقى 1434. اتصلحت ثغرات الأصل (شوف "Identity Endpoints"). اتجرّب كامل في docker compose. |
| إضافة BuildingBlocks (3 مشاريع) | `BuildingBlocks` و `BuildingBlocks.Localization` و `BuildingBlocks.Messaging` على نفس شكل اللي في AvocadoERP. الـ Application بيستخدم الـ MediatR behaviors؛ والـ API بيستخدم الـ exception handler والـ localization؛ والـ Dockerfile بينسخ `BuildingBlocks/`. الـ Messaging لسه مش متوصل (مفيش RabbitMQ). |
| إضافة Carter و Mapster و MediatR و FluentValidation و EF Core | كل حزمة في الطبقة المناسبة (الجدول فوق). |
| إضافة تسجيل الخدمات | ملفات `GlobalUsing.cs` و `ServiceInjection.cs` في API و Application و Infrastructure؛ الربط في `Program.cs`؛ إضافة DbContext والـ connection string. |
| نقل `Users` إلى `Entities/Identity` وإضافة كلاسات كل جداول Identity | الـ namespace بقى `ServicesA.Domain.Entities.Identity`؛ `AppDbContext` بيستخدم الكلاسات الصريحة؛ وفيه configuration لكل جدول. |
| حل تحذيرات الثغرات في NuGet | تثبيت `System.Security.Cryptography.Pkcs` 10.0.14 في Infrastructure (NU1902/NU1903). |
| إنشاء migration باسم `InitialCreate` | إضافة `AppDbContextFactory`؛ وإصلاح JSON غلط (backslash من غير escape) في `appsettings.json`. لسه متطبقتش على قاعدة البيانات. |
| ضبط باسورد `sa` | الباسورد اتنقل لملف `.env` المتجاهل من Git (`MSSQL_SA_PASSWORD`)؛ ويوزر `sa` مفعّل في الكونتينر. |
| إضافة SQL Server في docker compose | خدمة `sqlserver` بـ volume و healthcheck؛ والـ API بيعتمد عليها وبيتصل بيها بـ connection string من الـ environment. |
| إضافة Serilog | `Serilog.AspNetCore` في API؛ sink للكونسول وملف يومي (`logs/servicesa-.log` لمدة 14 يوم) من `appsettings.json`؛ مستوى Debug في Development؛ تسجيل الـ requests؛ و `logs/` متجاهل في Git. |
| إضافة Identity وجداول الهيستوري ومبادئ DDD | `AppDbContext` و `IAppDbContext` وملفات الـ configuration و `AuditLog` و `LoginHistory` و `ApplicationUser`/`ApplicationRole`. حلّت محل `ApplicationDbContext`. الـ migration لسه متعملتش (Application Control حاجبها). |

### Identity وجداول الهيستوري و DDD
- **Domain** (من غير اعتماد على EF): فولدر `Common/` فيه `Entity` و `AggregateRoot` و `ValueObject` و `IDomainEvent`؛ فولدر `Entities/Identity/` فيه `ApplicationUser` (aggregate root بمفتاح Guid وبـ factory اسمه `Create` وبيطلّع `UserRegisteredDomainEvent`) و `ApplicationRole` و `FullName` (value object) وكلاس لكل جدول Identity باقي: `ApplicationUserRole` و `ApplicationUserClaim` و `ApplicationUserLogin` و `ApplicationUserToken` و `ApplicationRoleClaim`؛ فولدر `Auditing/` فيه `AuditLog` (هيستوري تغيّر البيانات) و `LoginHistory` (هيستوري محاولات الدخول).
- **Application**: `IAppDbContext` بيعرض `Users` و `Roles` و `AuditLogs` و `LoginHistories` و `SaveChangesAsync`.
- **Infrastructure**: `AppDbContext` بينشئ جداول Identity الجاهزة (`AspNetUsers` و `AspNetRoles` وباقيهم). عند الحفظ بيملأ `CreatedAtUtc`/`ModifiedAtUtc` وبيكتب سجل `AuditLog` لكل كيان اتغير (من غير الـ password hash). ملفات الـ configuration في `Persistence/Configurations/`.
- الـ domain events بتتسجل على الـ aggregate بس لسه مش بتتبعت.
- أداة `dotnet-ef` مثبتة في [dotnet-tools.json](dotnet-tools.json). أمر الـ migration (من غير تطبيق على قاعدة البيانات):
```bash
dotnet dotnet-ef migrations add InitialCreate --project Services/servicesA/ServicesA.Infrastructure --startup-project Services/servicesA/ServicesA.API --output-dir Persistence/Migrations
```
- **الحالة:** الـ migration `InitialCreate` (في `Persistence/Migrations`) فيها أعمدة تسجيل الدخول، وبتتطبق تلقائيًا عند التشغيل لو `Database:InitializeOnStartup` بـ true (مفعّلة في Development وفي docker compose). على الجهاز ده Windows Application Control بيمنع `dotnet ef` (خطأ 0x800711C7)، فالـ migration اتعملت جوه صورة .NET SDK في Docker (الأمر موجود في النسخة الإنجليزية فوق).

### Identity Endpoints
جرّبهم من Swagger UI: http://localhost:5000/swagger (في Development). نفس الـ routes بتاعت AvocadoERP، مبنية بـ Carter -> MediatR -> handlers في الـ Application. تسجيل الدخول على خطوتين: `login` بيتحقق من الباسورد وبيبعت كود من 6 أرقام على الإيميل، وبعدها `verify-otp` بيرجّع الـ JWT.

| Method | Route | الصلاحية |
|--------|-------|----------|
| POST | `/api/Account/login` | من غير تسجيل، و rate limited |
| POST | `/api/Account/verify-otp` | من غير تسجيل، و rate limited |
| POST | `/api/Account/resend-otp` | من غير تسجيل، و rate limited (بس بعد login اتحقق من الباسورد) |
| POST | `/api/Account/forgot-password` | من غير تسجيل، و rate limited (بيرجّع `true` دايمًا) |
| POST | `/api/Account/reset-password` | من غير تسجيل، و rate limited |
| POST | `/api/Account/logout` | مسجّل دخول |
| POST | `/api/Account/refresh-session` | مسجّل دخول (بيجدد الجلسة الحالية وبيرجّع توكن جديد) |
| POST | `/api/Account/change-password` | مسجّل دخول (اليوزر من التوكن) |
| POST | `/api/Account/register` | `RegisterUser` أو `Administrator` |
| POST | `/api/Account/registerUserFull` | `Administrator` (يوزر + صلاحيات) |
| POST | `/api/Account/editUser` | `EditUser` أو `Administrator` |
| GET | `/api/Account/getAllUserList?pageNumber=&pageSize=&findValues=` | `GetAllUsers` أو `Administrator` |
| GET | `/api/Account/getOTP?fullName=` | **Development بس** (مش موجود في أي بيئة تانية) |
| POST | `/api/Roles/createRole` | `CreateRole` أو `Administrator` |
| GET | `/api/Roles/getAllRolesList` | `GetAllRolesList` أو `Administrator` |
| POST | `/api/UserRoles/createUserRole` | `CreateUserRole` أو `Administrator` |
| GET | `/api/UserRoles/getAllUserRolesList/{userId}` | `GetAllRolesList` أو `Administrator` |
| PUT | `/api/UserRoles/setUserRoles` | `Administrator` (بيبدّل الصلاحيات؛ ماعدا صلاحية Administrator بتاعتك) |
| GET | `/api/UserGroups/getAllGroups?pageNumber=&pageSize=&findValues=` | `GetAllRolesList` أو `Administrator` |
| GET | `/api/UserGroups/{id}` (الصلاحيات والأعضاء) | `GetAllRolesList` أو `Administrator` |
| GET | `/api/UserGroups/user/{userId}` | `GetAllRolesList` أو `Administrator` |
| POST | `/api/UserGroups/createGroup` | `Administrator` |
| PUT | `/api/UserGroups/updateGroup` | `Administrator` |
| DELETE | `/api/UserGroups/{id}` | `Administrator` |
| PUT | `/api/UserGroups/setUserGroups` | `Administrator` |
| GET | `/api/Configuration/email-settings` | `Administrator` (من غير الباسورد، بس `hasPassword`) |
| PUT | `/api/Configuration/email-settings` | `Administrator` (الباسورد بيتشفر قبل الحفظ) |
| POST | `/api/Configuration/test-email` | `Administrator`، و rate limited |

**سيرفر الإيميل:** بيتحفظ في قاعدة البيانات من الإعدادات > الإيميل (والباسورد متشفر). ولحد ما حاجة تتحفظ هناك بيستخدم الاحتياطي اللي تحت. **الإيميل في التطوير:** docker compose بيشغّل **Mailpit**؛ وكل إيميل (أكواد OTP ولينكات تغيير الباسورد والإيميلات التجريبية) بيوصل الـ inbox بتاعه على http://localhost:8025 ومفيش حاجة بتتبعت فعلًا. عشان تستخدم سيرفر حقيقي حط دول في `.env` وشغّل `docker compose up -d`: `SMTP_SERVER` و `SMTP_PORT` و `SMTP_SECURITY` (`Auto` أو `StartTls` أو `SslOnConnect` أو `None`) و `SMTP_USERNAME` و `SMTP_PASSWORD` و `SMTP_SENDER` (واسم المرسل هو اسم الشركة من إعدادات المشروع؛ و `SMTP_SENDER_NAME` احتياطي بس) (ولو عايز `RESET_PASSWORD_URL`، والافتراضي `http://localhost:4200/auth/reset-password`). وبعدها جرّبه من الإعدادات > الإيميل > "ابعت إيميل تجريبي". ملف `.env` متجاهل في Git، فباسورد الـ SMTP عمره ما بيترفع؛ وهو دلوقتي متظبط على Hostinger (`smtp.hostinger.com:465` بـ SSL). ولو شغلت الـ API من غير docker (`dotnet run`) فولدر `emails/` لسه شغال.

بيشتغل إزاي:
- **Domain**: `ApplicationUser` هو اللي فيه القواعد: `IssueOtp` / `TryConsumeOtp` (كود صلاحيته 5 دقايق، وأقصى 5 محاولات غلط، ومقارنة ثابتة الوقت)، و `StartSession` / `EndSession` / `IsSessionValid` (جلسة واحدة نشطة)، و `UpdateProfile`، و `Activate` / `Deactivate`. الأعمدة الجديدة: `DateOfBirth` و `LastActiveAtUtc` و `OtpCode` و `OtpExpiresAtUtc` و `OtpFailedAttempts` و `ActiveSessionId` و `ActiveSessionExpiresAtUtc`، و `AspNetRoles.Title`. أسماء الصلاحيات في `IdentityRoles`.
- **JWT**: فيه `sub` و `name` و `email` و `role` و `sid` (الجلسة) و `security_stamp`. مع كل request بيتأكد إن الجلسة لسه هي النشطة وإن الـ security stamp متغيرش، فالـ logout وتسجيل الدخول الإجباري وتغيير/استعادة الباسورد وإلغاء التفعيل بيأثروا فورًا. الإعدادات في قسم `JWT` (`Key` لازم 32 byte على الأقل وبيتفحص عند التشغيل؛ فيه key للتطوير بس في `appsettings.Development.json`، والـ production لازم يحدد `JWT__Key`).
- **جلسة واحدة**: لو حد سجل دخول والجلسة نشطة بيرجع 400 ومعاه `errorCode: "USER_ALREADY_LOGGED_IN"`؛ ابعت `"forceLogin": true` عشان تاخد الجلسة.
- **القفل**: 5 باسوردات غلط بيقفلوا الحساب 5 دقايق. شروط الباسورد: 8 حروف على الأقل فيها حرف كبير وصغير ورقم ورمز.
- **الإيميل** (MailKit): قسم `EmailSettings`. لو `SmtpServer` فاضي الإيميلات بتتسجل في اللوج بس، وفي Development بتقرأ الكود من `getOTP`. الباسورد عمره ما بيتبعت في إيميل.
- **الهيستوري**: كل محاولة باسورد/OTP بتضيف صف في `LoginHistories` (IP و user agent والنتيجة). و `AuditLogs` مش بتسجل `PasswordHash` و `SecurityStamp` و `ConcurrencyStamp` و `OtpCode` و `ActiveSessionId`.
- **عند التشغيل** (`DatabaseInitializer`): بيطبق الـ migrations، وبيعمل الصلاحيات اللي في `IdentityRoles`، وبيعمل أول Administrator من `Seed:Admin` لو `Seed:Admin:Password` موجود (docker compose بياخده من `ADMIN_PASSWORD` في `.env`؛ واليوزر `Administrator` / `omar-fathy@outlook.com`، والاسم وتاريخ الميلاد والموبايل من `Seed__Admin__*` في `docker-compose.yml`). وقاعدة البيانات اسمها `ServiceDb`.
- **الرسائل** بتتبع `Accept-Language` (`en`/`ar`) ومعاها رسائل FluentValidation. والأخطاء بترجع ProblemDetails؛ والـ JSON الغلط بيرجع 400.

الفرق عن نسخة AvocadoERP (أمان):
- `getOTP` كان مفتوح لأي حد وبيرجّع كود أي يوزر؛ بقى Development بس.
- `register` و `editUser` و `createRole` و `createUserRole` كان الـ `[Authorize]` بتاعهم متعلّق، يعني أي حد يقدر يعمل يوزر أو يدي نفسه Administrator؛ دلوقتي محتاجين صلاحيات.
- الباسورد مبقاش بيتبعت في إيميل (الأصلي كان بيبعت نسخة كمان لإيميل ثابت في الكود).
- الـ logout وتغيير/استعادة الباسورد وإلغاء التفعيل بقوا بيلغوا التوكن فعلًا؛ الأصلي كان بيمسح حقل بس والتوكن يفضل شغال.
- الـ OTP بقى بـ `RandomNumberGenerator` بدل `Random`، و `verify-otp` و `reset-password` بقوا rate limited.
- اليوزر أو الإيميل اللي مش موجود بياخد نفس رد اليوزر الغلط.
- فحص شهادة الـ SMTP شغال، و issuer/audience بتوع الـ JWT من الإعدادات بدل `"SecureApi"` الثابتة.
- شيلت الـ Branches والصور لأنهم مش موجودين في المشروع ده.
- الـ body بيستخدم `firstName`/`lastName` (الـ value object `FullName`) بدل `fullName` واحد، واللغة من header `Accept-Language` بدل حقل `currentLanguage`.

### BuildingBlocks
مشاريع مشتركة جوه `BuildingBlocks/`، والسيرفسز بتشاور عليها.

| المشروع | المحتوى | مين بيستخدمه |
|---------|---------|---------------|
| `BuildingBlocks` (MediatR 15 و FluentValidation 12.2) | `CQRS/`: `ICommand` و `ICommandHandler` و `IQuery` و `IQueryHandler`. `Behaviors/`: `LoggingBehavior` (بيسجل كل request وبيحذّر لو أخد أكتر من 3 ثواني) و `ValidationBehavior` (بيشغّل الـ validators للـ commands وبيرمي `ValidationException`) و `Sorting`. `Exceptions/`: `BadRequestException` و `NotFoundException` و `InternalServerException` و `Handler/CustomExceptionHandler` (بيرجّع ProblemDetails بـ 400/404/500 ومعاه `traceId` و `ValidationErrors`). `Options/JwtOptions` (قسم `JWT`). `DependencyInjection`: `AddBuildingBlocksBehaviors()` و `AddCustomExceptionHandler()`. | Application (الـ behaviors) و API (الـ exception handler و `app.UseExceptionHandler()`) |
| `BuildingBlocks.Localization` | `ILanguageService` (بيقرأ `Accept-Language` وبيدعم `en`/`ar` والافتراضي `en`) و `IStringLocalizerService.Get(key)` و `ResourceKeys` وملفات `Resources/SharedResources.en.resx` و `.ar.resx`. بيتسجل بـ `AddLocalizationDependencyInjection()`. | API |
| `BuildingBlocks.Messaging` (MassTransit.RabbitMQ 8.5.12) | `IntegrationEvent` كـ base record، و `AddMessageBroker(configuration, assembly)` بيقرأ `MessageBroker:Host` و `UserName` و `Password`. | لسه مش متوصل |

### إعدادات المشروع (الباك إند)
صف واحد فيه إعدادات السيستم كله اللي الواجهة بتستخدمها، ومتخزن في جدول `ProjectConfigurations`.

| Method | Route | الصلاحية |
|--------|-------|----------|
| GET | `/api/Configuration` | من غير تسجيل (صفحة الدخول محتاجة اسم الشركة والألوان والخطوط)؛ وبيرجّع القيم الافتراضية لحد ما حاجة تتحفظ |
| PUT | `/api/Configuration` | `Administrator` |

- **Domain** (`Entities/Settings/`): `ProjectConfiguration` (aggregate root و auditable) ومعاه value objects: `CompanyDetails` (الاسم إنجليزي/عربي، والإيميل، والتليفون، والعنوان، والموقع، والرقم الضريبي، واللوجو)، و `CurrencySettings` (كود ISO، والرمز، ومن 0 لـ 4 كسور)، و `ThemeColors` (اللون الأساسي وخلفية الفاتح والداكن بصيغة `#RRGGBB`)، و `Typography` (أسماء خطوط Google للإنجليزي والعربي، والحجم الأساسي من 12 لـ 20 px)، وكمان `DefaultLanguage` (`en`/`ar`) و `DefaultTheme` (`light`/`dark`). متخزنين بـ EF complex properties.
- **التحقق**: أسماء الخطوط حروف وأرقام ومسافات بس، والألوان لازم hex، عشان يبقوا آمنين جوه الـ CSS؛ واللوجو لازم لينك http(s) أو صورة PNG/JPEG/WEBP/GIF كـ data URL لحد حوالي 200 كيلوبايت (الـ SVG مرفوض)؛ والموقع لازم http(s).
- **CORS**: `Cors:AllowedOrigins` في `appsettings.json` (الافتراضي `http://localhost:4200`)؛ والـ headers `Pagination` و `Retry-After` ظاهرين للواجهة.

### واجهة Angular
Angular 21 (standalone components و signals و zoneless) في فولدر `UI/`. بتكلم الـ API على `http://localhost:5000` (`src/environments/`).

```bash
cd UI
npm install
npm start        # http://localhost:4200 (الـ API لازم يكون شغال: docker compose up -d)
npm run build    # بناء production في UI/dist/
```

| المطلوب | مكانه |
|---------|-------|
| تسجيل الدخول | `features/auth/login`: اسم المستخدم والباسورد -> `POST /api/Account/login`. لو الحساب مفتوح في مكان تاني (`USER_ALREADY_LOGGED_IN`) بيعرض "ادخل من هنا واقفل الجلسة التانية" (`forceLogin`). |
| التحقق بالـ OTP | `features/auth/verify-otp`: كود من 6 أرقام -> `POST /api/Account/verify-otp`، وبيتبعت لوحده أول ما الرقم السادس يتكتب، وبيقبل لصق الكود، وإعادة الإرسال بعد عداد 30 ثانية. محتاج الخطوة الأولى الأول (`otpGuard`). في Development اقرا الكود من `GET /api/Account/getOTP?fullName=` في Swagger لأن مفيش SMTP متظبط. |
| لوحة التحكم | `features/dashboard`: عدد المستخدمين والصلاحيات (بينادي الـ API بس لو اليوزر عنده الصلاحية)، وصلاحياتي، ووقت انتهاء الجلسة، والبروفايل، والشركة وشكل العملة، وجدول المستخدمين. |
| القائمة الجانبية والـ Nav Bar | `layout/`: قائمة جانبية بتتصغر (وبتفتكر حالتها)، وعلى الموبايل بتطلع فوق الصفحة؛ والعناصر بتتفلتر حسب الصلاحية. الـ Nav Bar: زرار القائمة، واللغة، والثيم، وقائمة اليوزر فيها تسجيل الخروج. |
| الثيم الفاتح والداكن | `ThemeService`: `data-theme` على `<html>`، ومتغيرات CSS في `src/styles/_tokens.scss`؛ بيتحفظ لكل متصفح، ولو مفيش بياخد الافتراضي بتاع المشروع. |
| العربي (RTL) والإنجليزي | `LanguageService` بيحط `lang`/`dir` على `<html>`؛ والنصوص في `core/i18n/translations.ts` مع الـ pipe اسمه `t`؛ وكل الـ CSS بـ logical properties فالشاشة بتتقلب لوحدها؛ وبيبعت `Accept-Language` عشان رسائل الـ API ترجع بنفس اللغة. |
| إعدادات المشروع | `features/settings/project-configuration` (للـ Administrator بس): بيانات الشركة ورفع اللوجو (بأي حجم وبيتصغر في المتصفح)، والعملة، والألوان (picker و hex)، والخطوط، واللغة والثيم الافتراضيين، مع معاينة مباشرة. `AppConfigService` بيحمّلها قبل ما التطبيق يبدأ (`provideAppInitializer`) وبيطبق الألوان والخطوط كمتغيرات CSS و Google Fonts. |
| نسيت الباسورد / تغييره من الإيميل | `features/auth/forgot-password` و `reset-password`. صفحة "نسيت الباسورد" بتعرض نفس الرسالة دايمًا (مفيش طريقة تعرف بيها مين الإيميلات المسجلة). في Development افتح أحدث ملف في `emails/` ودوس على الزرار. |
| حسابي | `features/account`: بياناتي وتغيير الباسورد (الحالي والجديد والتأكيد). الـ API بيقفل كل الجلسات بعد التغيير، فالواجهة بتعمل تسجيل خروج وبتطلب دخول بالباسورد الجديد. |
| مجموعات المستخدمين | `features/groups` (العرض: Administrator أو `GetAllRolesList`؛ التعديل: Administrator): قائمة فيها الصلاحيات وعدد الأعضاء، وإضافة/تعديل (الاسم والوصف والصلاحيات؛ والأعضاء بيظهروا)، ومسح بعد تأكيد. ومجموعات المستخدم بتتحدد من صفحة المستخدمين (زرار "المجموعات") أو وقت إضافة المستخدم. |
| الإيميل | `features/settings/email-settings` (للـ Administrator): الطريقة الحالية (SMTP / ملفات / مقفول)، والسيرفر، والأمان، والمرسل، ولينك inbox بتاع Mailpit، و"ابعت إيميل تجريبي" بالنتيجة الحقيقية أو خطأ الـ SMTP. |
| الصلاحيات | `features/roles` (للـ Administrator أو `GetAllRolesList`): قائمة فيها بحث وتقسيم صفحات؛ و"إضافة صلاحية" للـ Administrator أو `CreateRole` (اسم الكود والعنوان والوصف). |
| المستخدمين | `features/users` (للـ Administrator أو `GetAllUsers`): قائمة فيها بحث وتقسيم صفحات، وإضافة (الباسورد بيتفحص حسب الشروط، والصلاحيات للـ Administrator)، وتعديل (البيانات والتفعيل)، وشاشة صلاحيات (`setUserRoles`). الزراير بتظهر بس حسب الصلاحيات اللي الـ API بيقبلها. وحسابك إنت مينفعش يتشال منه Administrator ولا يتوقف. |
| Lazy loading | `app.routes.ts`: `auth` و `dashboard` و `users` و `roles` و `account` و `settings` كل واحد chunk لوحده بيتحمّل بـ `loadChildren`؛ والـ guards بتاعت `canMatch` بتشتغل قبل تحميل الـ chunk. |
| JWT و Interceptors و Guards | `AuthService` بيحفظ التوكن (localStorage) ووقت انتهائه من الـ claim اسمه `exp`؛ وقبل النهاية بـ 5 دقايق `layout/session-expiry` بيسأل لو عايز تجدد (`refresh-session`)، ولو لأ بيعمل تسجيل خروج لما يخلص. الـ Interceptors: `languageInterceptor` (Accept-Language)، و `authInterceptor` (Bearer token لعنوان الـ API بس)، و `errorInterceptor` (401 -> تسجيل خروج، و 403/5xx/مشكلة شبكة -> رسالة). الـ Guards: `authGuard` و `guestGuard` و `otpGuard` و `roleGuard(...)`. |

الباسورد بيتحفظ في الذاكرة بس بين الـ login والـ verify-otp، لأن `verify-otp` في الـ API بيتحقق منه تاني. والتحويل بعد الدخول (`returnUrl`) بيقبل مسارات جوه التطبيق بس.

الفرق عن نسخة AvocadoERP: `LoggingBehavior` بيحسب `TotalSeconds` (الأصلي كان بيستخدم `Seconds` اللي بتتجاهل الدقايق)؛ و `IntegrationEvent.Id`/`OccurredOnUtc` بيتحددوا مرة واحدة (الأصلي كان بيعمل قيمة جديدة مع كل قراءة)؛ والـ localization بتقرأ اسم الـ resource الصح وبتتعامل مع headers زي `ar-EG,ar;q=0.9`؛ و `JWT` بقى `Options/JwtOptions`؛ وشيلت `BasketCheckoutEvent` الخاص بمشروع تاني والحزم اللي مش مستخدمة (Newtonsoft.Json و FeatureManagement و Mapster و EF Core)؛ و MassTransit على 8.x لأن 9.x محتاجة license تجاري.

### البناء والتشغيل
```bash
dotnet build SystemArchitecture.slnx
dotnet run --project Services/servicesA/ServicesA.API
docker compose up --build
```
