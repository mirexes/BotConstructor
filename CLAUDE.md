# CLAUDE.md - BotConstructor Platform

## Project Overview

BotConstructor is a no-code chatbot builder platform for creating messenger bots without programming. Built with ASP.NET Core MVC 9.0, C# 12, Entity Framework Core 9.0, and MySQL 8.0. The project documentation and UI are in Russian.

**Current status:** MVP 1.1 - Email/Password Authentication module (fully implemented), Admin panel, User dashboard/profile.

## Repository Structure

```
BotConstructor/
├── src/
│   ├── Core/                        # Domain layer (entities, interfaces, DTOs)
│   │   ├── Entities/                # Database entity models (BaseEntity, User, Role, etc.)
│   │   ├── Interfaces/              # Service contracts (IAuthService, IUserRepository, etc.)
│   │   ├── DTOs/                    # Data Transfer Objects with validation attributes
│   │   └── Models/                  # ServiceResult and other shared models
│   ├── Infrastructure/              # Implementation layer
│   │   ├── Data/                    # ApplicationDbContext (EF Core, seeds 5 roles)
│   │   ├── Repositories/           # UserRepository (IUserRepository impl)
│   │   └── Services/               # AuthService, EmailService, AdminService, ProfileService
│   ├── Web/                         # ASP.NET Core MVC presentation layer
│   │   ├── Controllers/            # AuthController, AdminController, ProfileController, HomeController
│   │   ├── Views/                  # Razor views (Auth/, Admin/, Profile/, Shared/)
│   │   ├── Models/                 # View models (AdminUsersViewModel, etc.)
│   │   ├── wwwroot/               # Static assets (Bootstrap 5, jQuery)
│   │   ├── Program.cs             # App configuration, DI, middleware pipeline
│   │   └── appsettings.json       # Connection string and logging config
│   └── BotConstructor.Platform.sln  # Solution file
├── tests/
│   ├── BotConstructor.Tests.Unit/        # Unit tests (xUnit + Moq + FluentAssertions)
│   ├── BotConstructor.Tests.Integration/ # Integration tests (WebApplicationFactory)
│   ├── BotConstructor.Tests.Security/    # Security-focused tests
│   └── BotConstructor.Tests.E2E/         # End-to-end tests
├── sql_auth_schema.sql              # Full database schema SQL script
└── TZ_BotConstructor_FULL_1_.md     # Technical specification (full requirements)
```

## Architecture

**Clean Architecture / Onion Architecture** with three layers:

- **Core** - Domain entities, interfaces, DTOs. Zero infrastructure dependencies.
- **Infrastructure** - EF Core DbContext, repository implementations, service implementations. Depends on Core.
- **Web** - Controllers, views, DI wiring, middleware. Depends on Core and Infrastructure.

**Key patterns:**
- Repository Pattern (`IUserRepository` -> `UserRepository`)
- Service Layer (`IAuthService` -> `AuthService`, etc.)
- Dependency Injection via ASP.NET Core built-in container
- Cookie-based authentication with Claims
- DTOs with Data Annotation validation

## Build & Run Commands

```bash
# Restore dependencies
cd src && dotnet restore

# Build the solution
dotnet build src/BotConstructor.Platform.sln

# Run the web application (listens on https://localhost:5001, http://localhost:5000)
dotnet run --project src/Web/BotConstructor.Web.csproj

# Run all tests
dotnet test

# Run specific test projects
dotnet test tests/BotConstructor.Tests.Unit
dotnet test tests/BotConstructor.Tests.Integration
dotnet test tests/BotConstructor.Tests.Security
dotnet test tests/BotConstructor.Tests.E2E
```

## Testing

- **Framework:** xUnit 2.6.3
- **Mocking:** Moq 4.20.70
- **Assertions:** FluentAssertions 6.12.0 (use `.Should()` style assertions)
- **Database:** `Microsoft.EntityFrameworkCore.InMemory` for test isolation
- **Integration tests:** `Microsoft.AspNetCore.Mvc.Testing` with `WebApplicationFactory`

Tests use in-memory database via `UseInMemoryDatabase()` for isolation. Each test should create its own `ApplicationDbContext` with a unique database name.

## Key Dependencies

| Package | Version | Layer |
|---------|---------|-------|
| .NET SDK | 9.0 | All |
| Entity Framework Core | 9.0.1 | Infrastructure |
| Pomelo.EntityFrameworkCore.MySql | 9.0.0 | Infrastructure |
| BCrypt.Net-Next | 4.0.3 | Infrastructure |
| Bootstrap | 5 | Web (frontend) |
| jQuery + jQuery Validation | - | Web (frontend) |

## Database

- **Engine:** MySQL 8.0+ with UTF8MB4 encoding
- **ORM:** Entity Framework Core (Code-First)
- **Migrations:** Applied automatically on startup via `db.Database.Migrate()` in `Program.cs`
- **Schema:** See `sql_auth_schema.sql` for reference

**Manual migration commands:**
```bash
cd src/Web
dotnet ef migrations add <MigrationName> --project ../Infrastructure/BotConstructor.Infrastructure.csproj --startup-project BotConstructor.Web.csproj
dotnet ef database update --project ../Infrastructure/BotConstructor.Infrastructure.csproj --startup-project BotConstructor.Web.csproj
```

**Key tables:** Users, Roles, UserRoles, LoginAttempts, PasswordResetTokens, EmailConfirmationTokens, UserSessions, UserSettings

**Seed data:** 5 system roles are seeded automatically (SuperAdmin, Admin, User, Developer, Affiliate).

## Security Conventions

These security practices are established and must be maintained:

- **Password hashing:** BCrypt with cost factor 12. Never store plaintext passwords.
- **Rate limiting:** Account lockout after 5 failed login attempts for 15 minutes.
- **CSRF protection:** All POST forms use `[ValidateAntiForgeryToken]` attribute.
- **Cookies:** HttpOnly, Secure, SameSite=Lax. 12-hour default expiry, 30 days with "Remember Me".
- **Email confirmation:** Required before a user can log in.
- **Token expiry:** Email confirmation tokens expire in 24 hours, password reset tokens in 1 hour.
- **Audit logging:** All login attempts (successful and failed) are logged with IP and user agent.
- **Input validation:** Data Annotations on DTOs, ModelState validation in controllers.

## Code Conventions

- **Language:** C# 12 with nullable reference types enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings:** Enabled in all projects
- **Target framework:** .NET 9.0
- **Service registration:** `AddScoped` for repositories and services, `AddSingleton` for `EmailService`
- **Controller pattern:** Each controller uses constructor injection for service interfaces
- **All entities** inherit from `BaseEntity` (provides `Id`, `CreatedAt`, `UpdatedAt`)
- **Service results:** Methods return `ServiceResult` (with `Success` bool and `Message` string)
- **Email in dev mode:** Emails are logged to console, not actually sent. See `EmailService.cs`.
- **UI documentation and strings** are in Russian (Cyrillic)

## DI Registration (Program.cs)

```csharp
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
```

When adding new services, follow this pattern: define the interface in `Core/Interfaces/`, implement in `Infrastructure/Services/`, register in `Program.cs`.

## URL Routing

Standard ASP.NET MVC convention routing: `{controller=Home}/{action=Index}/{id?}`

Key routes:
- `/Auth/Register`, `/Auth/Login`, `/Auth/Logout`
- `/Auth/ConfirmEmail?token=TOKEN`, `/Auth/ForgotPassword`, `/Auth/ResetPassword`
- `/Admin/Users` (admin panel, requires admin role)
- `/Profile/Settings`, `/Profile/Sessions` (user dashboard)

## What's Not Yet Implemented

Per the technical specification (`TZ_BotConstructor_FULL_1_.md`), the following MVP phases remain:
- Tier system and billing (section 2.x)
- Bot constructor (section 3.x)
- Telegram integration (section 4.x)
- Analytics (section 5.x)
