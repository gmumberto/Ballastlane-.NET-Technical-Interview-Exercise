# TaskManagement API

A RESTful task-management API built with **ASP.NET Core / .NET 10**, **C#**, **Entity Framework Core**, and **SQL Server**. It provides JWT authentication and user-scoped task management, including pagination, filtering, and sorting. The solution separates HTTP concerns, application use cases, domain entities, infrastructure adapters, and dependency composition.

## Features

- Registration and login with JWT Bearer (HS256) authentication
- Password hashing using ASP.NET Core Identity `IPasswordHasher<User>`
- Password policy: 12–128 characters, selected common-password rejection, and repeated-character rejection
- Task CRUD restricted to the authenticated owner
- Pagination, optional status/title filters, and sorting by due date, creation date, or title
- FluentValidation for API requests
- **Result Pattern** (`Result<T>`, `AppError`, `ErrorType`) for expected application outcomes
- RFC 9457-style Problem Details responses for mapped errors; global handler for unexpected exceptions
- Swagger/OpenAPI with operation-specific JWT requirements
- Optional, idempotent demo-user/task seeding in Development
- Multi-stage Docker image and Docker Compose with SQL Server 2022
- Unit and API integration tests, including cross-user isolation and concurrent-registration scenarios

## Technology stack

| Area | Technology |
| --- | --- |
| API | ASP.NET Core Web API, .NET 10, C# |
| Architecture | Clean Architecture-inspired layering, separate Bootstrap composition project |
| Persistence | EF Core, SQL Server, EF migrations |
| Authentication | JWT Bearer HS256, ASP.NET Core Identity password hasher |
| Validation | FluentValidation |
| Error handling | `Result<T>`, typed application errors, Problem Details, global exception handler |
| Documentation | Swashbuckle / Swagger / OpenAPI, XML comments |
| Testing | xUnit, ASP.NET Core `WebApplicationFactory`, SQLite in-memory |
| Containers | Docker, Docker Compose, SQL Server 2022 Developer image |

## Repository structure

The solution file, Docker files, and project folders are located inside the **`TaskManagement.API/` solution directory** of the archive:

```text
TaskManagement.API/                         # Solution root
├── TaskManagement.API.slnx
├── TaskManagement.API/                     # ASP.NET Core HTTP host
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── TasksController.cs
│   │   └── ResultHttpMapper.cs
│   ├── Filters/ValidationFilter.cs
│   ├── Middleware/GlobalExceptionHandler.cs
│   ├── Swagger/SwaggerAuthorizationOperationFilter.cs
│   ├── Properties/launchSettings.json
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── TaskManagement.API.csproj
├── TaskManagement.Bootstrap/               # Dependency composition and demo seeding
│   ├── DependencyInjection.cs
│   ├── DevelopmentSeeder.cs
│   └── TaskManagement.Bootstrap.csproj
├── TaskManagement.Application/             # Use cases and abstractions
│   ├── Common/
│   │   ├── Result.cs                       # Result<T>, AppError, ErrorType
│   │   ├── PasswordPolicy.cs
│   │   └── PagedResult.cs
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Services/
│   │   ├── AuthService.cs
│   │   └── TaskService.cs
│   ├── Validators/
│   ├── Exceptions/                          # Legacy exception type; review before removal
│   └── TaskManagement.Application.csproj
├── TaskManagement.Domain/
│   ├── Entities/                            # User and Task
│   ├── Enums/TaskStatus.cs
│   └── TaskManagement.Domain.csproj
├── TaskManagement.Infrastructure/
│   ├── Authentication/                      # JWT generator
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   └── Configurations/
│   ├── Repositories/
│   ├── Migrations/
│   └── TaskManagement.Infrastructure.csproj
├── TaskManagement.API.Tests/
│   ├── Controllers/                         # Auth, security, validation, paging, filtering
│   ├── Infrastructure/CustomWebApplicationFactory.cs
│   └── TaskManagement.API.Tests.csproj
├── TaskManagement.Application.Tests/
│   ├── Services/
│   ├── Validators/
│   └── TaskManagement.Application.Tests.csproj
├── TaskManagement.Domain.Tests/
│   ├── Entities/
│   └── TaskManagement.Domain.Tests.csproj
├── TaskManagement.Infrastructure.Tests/
│   └── TaskManagement.Infrastructure.Tests.csproj
├── Dockerfile
├── compose.yaml
├── .dockerignore
├── .env.example
├── ARCHITECTURE-REFACTOR.md
├── IMPLEMENTATION-STAGES-1-3.md
└── RESULT-PATTERN-REFACTOR.md
```

### Architectural responsibilities and dependencies

- **Domain** defines core entities and task status without HTTP or database responsibilities.
- **Application** defines DTOs, repository/service interfaces, validation, use cases, and typed results.
- **Infrastructure** implements repositories, EF Core persistence, and JWT generation.
- **Bootstrap** registers concrete implementations (`AddTaskManagementCore`) and contains the optional Development seeder.
- **API** configures the HTTP pipeline, authentication/authorization, controllers, validation filter, Swagger, and HTTP error mapping.
- **Tests** exercise domain logic, application services, and API behavior.

The API project references **Application and Bootstrap**, not Infrastructure directly in its project file. Bootstrap references Application and Infrastructure. This keeps concrete dependency registrations outside `Program.cs`, while Infrastructure remains a **transitive runtime dependency** of the host.

```text
API ────────────────> Application ───────> Domain
 │
 └──> Bootstrap ────> Infrastructure ────> Application / Domain
```

## Prerequisites

For local execution:

- .NET **10 SDK**
- SQL Server instance, or Docker Desktop / Docker Engine with Compose
- Valid SQL Server connection string
- Base64-encoded JWT signing key of at least **32 decoded bytes**

For container execution, Docker with Compose is sufficient; you do not need a locally installed .NET SDK to build the container image.

## Configuration

Configuration is loaded from ASP.NET Core configuration providers. Use user secrets for local execution and environment variables for containers. **Never commit actual credentials, signing keys, or a populated `.env` file.**

| Configuration key | Environment variable | Description |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt:Issuer` | `Jwt__Issuer` | Expected JWT issuer |
| `Jwt:Audience` | `Jwt__Audience` | Expected JWT audience |
| `Jwt:Key` | `Jwt__Key` | Base64 signing key (≥32 decoded bytes) |
| `Demo:Enabled` | `Demo__Enabled` | Opt-in development demo seeding |
| `Demo:Email` | `Demo__Email` | Demo account email |
| `Demo:Password` | `Demo__Password` | Demo account password (must pass policy) |

### Local development with user secrets

From the **solution root** (`TaskManagement.API/` containing `.slnx`):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=TaskManagementDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;" --project TaskManagement.API
dotnet user-secrets set "Jwt:Issuer" "TaskManagement.API" --project TaskManagement.API
dotnet user-secrets set "Jwt:Audience" "TaskManagement.Client" --project TaskManagement.API
$key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "Jwt:Key" $key --project TaskManagement.API
```

The sample SQL Server connection string assumes local Windows authentication. Change it for your database and platform. `TrustServerCertificate=True` is for appropriate development scenarios, not a production security default.

To enable demo data locally, also set:

```powershell
dotnet user-secrets set "Demo:Enabled" "true" --project TaskManagement.API
dotnet user-secrets set "Demo:Email" "demo@example.com" --project TaskManagement.API
dotnet user-secrets set "Demo:Password" "YOUR_UNIQUE_STRONG_DEMO_PASSPHRASE" --project TaskManagement.API
```

Replace the placeholder with a unique password that passes the policy. There are **no hard-coded usable demo credentials** in this README.

## Database setup and migrations

The Infrastructure project contains an initial EF Core migration. If you are running the API locally **without** the demo seeder, apply migrations explicitly before the first run:

```powershell
dotnet tool install --global dotnet-ef   # Skip if already installed
dotnet ef database update --project TaskManagement.Infrastructure --startup-project TaskManagement.API
```

When running in Development with `Demo:Enabled=true`, `DevelopmentSeeder` calls `MigrateAsync()` before checking/creating the demo user. The application does **not** automatically apply migrations when demo seeding is disabled.

## Run locally

From the solution root:

```powershell
dotnet restore
dotnet build
dotnet run --project TaskManagement.API
```

When `ASPNETCORE_ENVIRONMENT=Development`, open the `/swagger` path at the listening URL printed in the terminal, e.g. `https://localhost:<port>/swagger`.

## Run with Docker Compose

The solution includes a multi-stage .NET 10 `Dockerfile`, a `.dockerignore`, and `compose.yaml` with `api` and `sqlserver` services. The SQL Server service uses the **2022 Developer** image and a persistent named volume. The API listens on **HTTP port 8080** inside the container and is published as `localhost:8080`.

From the solution root:

1. Copy `.env.example` to `.env`.
2. Replace `SQL_SA_PASSWORD`, `JWT_KEY_BASE64`, and `DEMO_PASSWORD` placeholders with valid strong secrets.
3. Run:

```powershell
docker compose up --build -d
docker compose ps
docker compose logs -f api
```

Open `http://localhost:8080/swagger` after startup. The supplied Compose file runs the API in **Development**, enables demo seeding, and waits for the SQL Server container health check. This setup is intended for **local demonstration**, not production deployment.

Generate a JWT key with PowerShell:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Stop containers without deleting the database volume:

```powershell
docker compose down
```

**Warning:** `docker compose down -v` deletes the SQL Server volume and its data.

## Demo data

Demo seeding runs **only when both** conditions hold:

- ASP.NET Core environment is `Development`.
- `Demo:Enabled` is `true` and `Demo:Password` passes the password policy.

The seeder applies migrations, checks whether the configured demo email already exists, and creates a demo user with two tasks (`Explore Swagger` and `Review architecture`) when absent. Repeated runs do not intentionally duplicate that user or its sample tasks. Credentials come from configuration; use the configured `Demo:Email` and `Demo:Password` to log in.

> Note: The `DevelopmentSeeder.cs` in the archived snapshot had a `Task` naming collision; the working-tree fix uses `System.Threading.Tasks.Task` explicitly for its asynchronous return type. Confirm this correction is present before building.

## Authentication and authorization

1. Register via `POST /api/auth/register`, or log in via `POST /api/auth/login` (including with the configured demo account).
2. Copy the `accessToken` from the response.
3. In Swagger UI, select **Authorize** and paste the token **without** the `Bearer ` prefix.
4. Call the protected `/api/tasks` endpoints.

JWT validation checks issuer, audience, signing key, signature, lifetime, and HS256 algorithm. The API uses the validated `sub` claim as the user identifier. Repository queries scope task reads and writes to the authenticated user, including when an ID belongs to another user.

### Password policy

Registration and demo seeding use `PasswordPolicy`: minimum **12**, maximum **128** characters; selected commonly guessed passwords and single-character repetitions are rejected. Passwords are stored as hashes, not plaintext. This is a basic application policy, **not** an external breached-password screening service.

## API endpoints

| Method | Endpoint | Access | Success | Common errors |
| --- | --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Public | `201` | `400`, `409` |
| `POST` | `/api/auth/login` | Public | `200` | `400`, `401` |
| `GET` | `/api/tasks` | JWT | `200` | `401` |
| `GET` | `/api/tasks/paged` | JWT | `200` | `400`, `401` |
| `GET` | `/api/tasks/{id}` | JWT | `200` | `401`, `404` |
| `POST` | `/api/tasks` | JWT | `201` | `400`, `401` |
| `PUT` | `/api/tasks/{id}` | JWT | `204` | `400`, `401`, `404` |
| `DELETE` | `/api/tasks/{id}` | JWT | `204` | `401`, `404` |

### Pagination, filtering, and sorting

`GET /api/tasks/paged` accepts:

| Parameter | Default | Rules |
| --- | --- | --- |
| `pageNumber` | `1` | Positive integer |
| `pageSize` | `10` | 1–100 |
| `status` | None | Optional valid `TaskStatus` enum value |
| `title` | None | Optional partial title, maximum 200 characters |
| `sortBy` | `dueDate` | `dueDate`, `createdAt`, `title` |
| `sortDirection` | `asc` | `asc`, `desc` |

Example:

```http
GET /api/tasks/paged?pageNumber=1&pageSize=10&title=JWT&sortBy=createdAt&sortDirection=desc
Authorization: Bearer <access-token>
```

The response includes `items`, `pageNumber`, `pageSize`, `totalCount`, and `totalPages`. Filtering is performed before counting and paging; ordering uses task ID as a deterministic tie-breaker. Title case sensitivity depends on the database collation/provider.

## Result Pattern and error handling

Application services return **`Result<T>`** for expected outcomes rather than throwing exceptions as ordinary business control flow. The shared contract contains:

- `IsSuccess` and `Value` for successful operations
- `Error` with `Type`, `Code`, and `Message` for failures
- `ErrorType` values: `Validation`, `NotFound`, `Conflict`, and `Unauthorized`

`ResultHttpMapper` maps typed failures to `400`, `404`, `409`, or `401` and emits `ProblemDetails` with `application/problem+json` and an application-specific `code` extension. The global exception handler remains responsible for unexpected exceptions. FluentValidation also produces structured validation responses for invalid request models.

**Important MVC detail:** The controllers must not have a class-level `[Produces("application/json")]` attribute that forces error responses to use `application/json`; it was removed in the working tree after an integration test exposed the mismatch. Keep `application/problem+json` for Problem Details.

## Swagger / OpenAPI

Swagger UI is available in Development at `/swagger`. XML comments describe controller actions, and `SwaggerAuthorizationOperationFilter` attaches the JWT requirement to endpoints with `[Authorize]` while leaving `[AllowAnonymous]` endpoints public. The Swagger security scheme is HTTP Bearer.

## Tests

Run all tests from the solution root:

```powershell
dotnet test
```

Run focused suites:

```powershell
dotnet test --filter "FullyQualifiedName~AuthControllerTests"
dotnet test --filter "FullyQualifiedName~TasksControllerSecurityTests"
dotnet test --filter "FullyQualifiedName~TasksPaginationTests"
dotnet test --filter "FullyQualifiedName~TasksFilteringTests"
```

The repository includes Domain, Application, Infrastructure, and API test projects. API integration tests use `WebApplicationFactory` with SQLite in-memory and cover authentication, request validation, user isolation, pagination, filtering, and HTTP error responses. The project has also exercised concurrent registration behavior. SQLite tests do not replace SQL Server-specific integration checks.

## Design decisions and limitations

- **Explicit application outcomes:** `Result<T>` represents expected validation, conflict, unauthorized, and not-found cases; exceptions remain for unexpected failures.
- **Separated composition:** `TaskManagement.Bootstrap` owns concrete registrations. The API still depends transitively on Infrastructure at runtime.
- **Ownership checks:** repository queries include `UserId` to prevent cross-user access.
- **Deterministic pagination:** a secondary ID sort stabilizes rows with equal primary sort values; offset pagination can still shift under concurrent writes.
- **Developer experience:** Swagger, XML comments, optional demo data, and Docker Compose simplify local evaluation.
- **Security boundaries:** demo seeding and Swagger UI are Development-only; Docker Compose is a local demonstration setup.
- **CI status:** GitHub Actions / automated CI **has not yet been implemented**. It is the planned next phase.
- **Verification status:** The archived source and subsequent user-reported fixes informed this README; a complete build/test/container verification of the final working tree is still required.

## License

No license is specified in the inspected repository. Add a `LICENSE` file if you intend to grant reuse rights.
