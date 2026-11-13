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
├── .gitignore / .dockerignore  # Ignore rules
├── BuildingBlocks/             # Shared code used across services (empty for now)
└── Services/
    └── servicesA/
        ├── ServicesA.API
        ├── ServicesA.Application
        ├── ServicesA.Domain
        └── ServicesA.Infrastructure
```

### Root files
| File | Purpose |
|------|---------|
| `SystemArchitecture.slnx` | The solution; groups all projects. |
| `docker-compose.yml` | Builds and runs `servicesa-api` (host port 5000 -> container port 8080). |
| `Directory.Build.props` | Applies `net10.0`, nullable reference types and implicit usings to every project. |
| `global.json` | Pins the SDK to 10.0.x (rolls forward to the latest feature band). |
| `.gitignore` | Excludes `bin/`, `obj/`, `.vs/`, `.idea/`, `*.user` from Git. |
| `.dockerignore` | Excludes `bin` and `obj` from the Docker build context. |

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
| API | Microsoft.EntityFrameworkCore.Design | 10.0.14 | design-time only (migrations) |
| Application | MediatR | 15.0.0 | `AddApplication()` -> `AddMediatR(...)` |
| Application | FluentValidation.DependencyInjectionExtensions | 12.2.0 | `AddApplication()` -> `AddValidatorsFromAssembly(...)` |
| Application | Mapster + Mapster.DependencyInjection | 10.0.14 | `AddApplication()` -> scans mappings, registers `IMapper` |
| Infrastructure | Microsoft.EntityFrameworkCore.SqlServer | 10.0.14 | `AddInfrastructure()` -> `AddDbContext<ApplicationDbContext>` |
| Domain | none | | |

Carter and EF Core are pinned to net10-compatible versions because their latest releases (11.x) target net11 only.

Each of API, Application and Infrastructure has two helper files:
- `GlobalUsing.cs`: global using directives for that layer.
- `ServiceInjection.cs`: the `Add...` extension method that registers the layer's services.

The connection string `DefaultConnection` is in [appsettings.json](Services/servicesA/ServicesA.API/appsettings.json) (SQL Server LocalDB by default). `ApplicationDbContext` is in `ServicesA.Infrastructure/Persistence`.

### Change log
| Change | Details |
|--------|---------|
| Added Carter, Mapster, MediatR, FluentValidation, EF Core | Each in its proper layer (see table above). |
| Added service registration | `GlobalUsing.cs` and `ServiceInjection.cs` in API, Application and Infrastructure; wired in `Program.cs`; added `ApplicationDbContext` and the connection string. |

### Build and run
```bash
dotnet build SystemArchitecture.slnx
dotnet run --project Services/servicesA/ServicesA.API
docker compose up --build
```

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
├── .gitignore / .dockerignore  # ملفات التجاهل
├── BuildingBlocks/             # كود مشترك بين السيرفسز (فاضي حاليًا)
└── Services/
    └── servicesA/
        ├── ServicesA.API
        ├── ServicesA.Application
        ├── ServicesA.Domain
        └── ServicesA.Infrastructure
```

### الملفات الأساسية
| الملف | الوظيفة |
|------|---------|
| `SystemArchitecture.slnx` | السوليشن، بيجمع كل المشاريع. |
| `docker-compose.yml` | بيبني ويشغل `servicesa-api` (بورت 5000 على الجهاز -> 8080 في الكونتينر). |
| `Directory.Build.props` | بيطبق `net10.0` و nullable و implicit usings على كل المشاريع. |
| `global.json` | بيثبت الـ SDK على 10.0.x (مع roll forward لأحدث إصدار). |
| `.gitignore` | بيستثني `bin/` و `obj/` و `.vs/` و `.idea/` و `*.user` من Git. |
| `.dockerignore` | بيستثني `bin` و `obj` من سياق بناء Docker. |

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
| API | Microsoft.EntityFrameworkCore.Design | 10.0.14 | وقت التصميم بس (الـ migrations) |
| Application | MediatR | 15.0.0 | `AddApplication()` -> `AddMediatR(...)` |
| Application | FluentValidation.DependencyInjectionExtensions | 12.2.0 | `AddApplication()` -> `AddValidatorsFromAssembly(...)` |
| Application | Mapster + Mapster.DependencyInjection | 10.0.14 | `AddApplication()` -> بيعمل scan للـ mappings ويسجل `IMapper` |
| Infrastructure | Microsoft.EntityFrameworkCore.SqlServer | 10.0.14 | `AddInfrastructure()` -> `AddDbContext<ApplicationDbContext>` |
| Domain | لا شيء | | |

Carter و EF Core مثبتين على إصدارات متوافقة مع net10 لأن أحدث إصداراتهم (11.x) بتدعم net11 بس.

كل طبقة من API و Application و Infrastructure فيها ملفين:
- `GlobalUsing.cs`: الـ global usings الخاصة بالطبقة.
- `ServiceInjection.cs`: الـ extension method `Add...` اللي بتسجل خدمات الطبقة.

الـ connection string باسم `DefaultConnection` موجود في [appsettings.json](Services/servicesA/ServicesA.API/appsettings.json) (SQL Server LocalDB افتراضيًا). و `ApplicationDbContext` في `ServicesA.Infrastructure/Persistence`.

### سجل التعديلات
| التعديل | التفاصيل |
|---------|----------|
| إضافة Carter و Mapster و MediatR و FluentValidation و EF Core | كل حزمة في الطبقة المناسبة (الجدول فوق). |
| إضافة تسجيل الخدمات | ملفات `GlobalUsing.cs` و `ServiceInjection.cs` في API و Application و Infrastructure؛ الربط في `Program.cs`؛ إضافة `ApplicationDbContext` والـ connection string. |

### البناء والتشغيل
```bash
dotnet build SystemArchitecture.slnx
dotnet run --project Services/servicesA/ServicesA.API
docker compose up --build
```
