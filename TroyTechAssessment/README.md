# TroyTechAssessment

ASP.NET Core/.NET 10 property-management application for managing properties and units, accepting rental applications, reviewing applications, and issuing leases.

## Requirements

- .NET 10 SDK
- SQL Server or SQL Server Express
- A SQL Server instance reachable by the application
- Optional: JetBrains Rider or Visual Studio

The project targets `net10.0`. The solution file is `TroyTechAssessment.sln`.

## Initial setup

1. Clone or open the repository.
2. Confirm that SQL Server is running.
3. Configure the database connection string in:

   - `TroyTechAssessment/appsettings.json`, or
   - `TroyTechAssessment/appsettings.Development.json`

   Use a real SQL Server password and do not commit credentials. A typical local SQL Server connection string is:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,1433;Database=PropertyManagement;User Id=sa;Password=<password>;TrustServerCertificate=True"
     }
   }
   ```

   For local development, environment variables or .NET user secrets are preferred over storing a password in source control. The application reads the connection string named `DefaultConnection`.

4. Restore dependencies:

   ```bash
   dotnet restore TroyTechAssessment.sln
   ```

5. Start the application:

   ```bash
   dotnet run --project TroyTechAssessment/TroyTechAssessment.csproj
   ```

   The configured launch profiles use:

   - HTTP: `http://localhost:5112`
   - HTTPS: `https://localhost:7059`

## Database initialization

On startup, `TroyTechAssessment/Program.cs`:

1. Applies all Entity Framework Core migrations with `Database.MigrateAsync()`.
2. Runs `DatabaseSeeder.SeedAsync(...)`.

The seed operation creates:

- Applicant and Manager roles.
- Seed manager and applicant users.
- Properties and manager-property assignments.
- Unit types and units.
- Applications across the supported statuses.
- Multiple applicants per application, including applicant-specific residence history. Additional applicants must be existing user accounts; the application stores their `UserId` and reads email addresses from `AspNetUsers`.
- Application status history.
- Lease data.

Seeding is intended to be idempotent. If the database contains partial seed data, startup stops with an error rather than mixing inconsistent records.

The seed users are generated with Bogus, so their exact usernames are generated at seed time. Seed passwords are defined in `TroyTechAssessment/Data/DatabaseSeeder.cs`; change them before using seeded accounts outside local development.

If a local database needs to be recreated, remove the `PropertyManagement` database using your SQL Server administration tool, then start the application again. The application will apply migrations and reseed the database.

## Run tests

Run the existing xUnit test suite with:

```bash
dotnet test TroyTechAssessment.sln --no-restore
```

The tests cover lease rules, residence date validation, application workflow metadata, manager-note constraints, and related business logic.

## Main application areas

| Area | Location |
|---|---|
| Startup and service registration | `TroyTechAssessment/Program.cs` |
| EF Core context and relationships | `TroyTechAssessment/Data/ApplicationDbContext.cs` |
| Database seed data | `TroyTechAssessment/Data/DatabaseSeeder.cs` |
| EF Core migrations | `TroyTechAssessment/Migrations/` |
| Unit/property management | `TroyTechAssessment/Pages/Index.cshtml.cs`, `TroyTechAssessment/Controllers/` |
| Application wizard | `TroyTechAssessment/Pages/Apply/` |
| Application list | `TroyTechAssessment/Pages/Applications/` |
| Application review and notes | `TroyTechAssessment/Controllers/ReviewController.cs` |
| Residence history modal CRUD | `TroyTechAssessment/Controllers/ResidenceController.cs` |
| Reusable paging grid | `TroyTechAssessment/ViewComponents/PagedGridViewComponent.cs` |
| Automated tests | `TroyTechAssessment.Tests/` |

## Roles and workflow

There are two roles:

- **Applicant**: browses available units, creates and edits Draft/Returned applications, saves drafts, submits applications, withdraws submitted applications, and views application history. Applicants can add existing user accounts as additional applicants through the application form; duplicate and primary-applicant entries are rejected, and each associated applicant may view/edit the application when ownership checks allow.
- **Manager**: manages assigned properties and units, claims and releases submitted applications, reviews claimed applications, manages leases, and adds or edits their own private manager notes. Managers can select applicants from a dropdown to review each applicant's information and residence history, but applicant information is read-only to managers.

Application statuses are:

`Draft`, `Submitted`, `Returned`, `Approved`, `Denied`, and `Withdrawn`.

Approved, Denied, and Withdrawn applications are terminal. Approval creates a twelve-month lease, subject to active-lease and lease-overlap checks.

## APIs and OpenAPI

Authenticated list endpoints are available through Razor Page handlers:

- Units JSON: `/Index?handler=UnitsJson`
- Applications JSON: `/Applications?handler=Json`

The hand-written OpenAPI document is available at:

```text
/api/openapi.json
```

The list endpoints support database-side filtering, sorting, paging, and page sizes of 10, 25, or 50.

## Configuration notes

- Development settings are in `TroyTechAssessment/appsettings.Development.json`.
- Do not commit SQL Server passwords or other secrets.
- HTTPS uses the normal ASP.NET Core development certificate when running the HTTPS profile. If the certificate is not trusted locally, run:

  ```bash
  dotnet dev-certs https --trust
  ```

- Startup migration application requires the configured database account to have permission to create or alter the database schema.

## EF Core migration notes

The repository contains manually maintained migration files because the development environment may not have the `dotnet-ef` global tool installed. If the model changes:

1. Update the entity/configuration code.
2. Create or update the migration using the repository's established migration approach.
3. Keep `ApplicationDbContextModelSnapshot.cs` synchronized with the model.
4. Start the application and verify migrations apply cleanly.

Do not edit or remove migrations that may already have been applied to a shared database. Add a new migration for schema changes instead.

## Troubleshooting

### The application cannot connect to SQL Server

- Confirm SQL Server is running and listening on the configured host/port.
- Verify the database username and password.
- Confirm TCP/IP is enabled if connecting to a local SQL Server instance through port `1433`.
- Check that the SQL Server account can create and modify the database.

### Startup reports partial seed data

The seeder detected some, but not all, expected seed records. Use a clean local database or restore the database to a consistent state before starting the application again.

### A migration reports a schema mismatch

Check the migration history table and the current database schema. Do not reset a shared database. For a disposable local database, recreate it and allow startup to apply all migrations from the beginning.

## Current scope

The application supports the complete multi-applicant workflow and the listed optional features, including paging/sorting, database-side application filters, JSON/OpenAPI list endpoints, review queue claims, manager note history, invalid draft saves, applicant-specific residence histories, and stale-save detection.

Applications can include multiple applicants. Each applicant must have an existing account and has an independent residence-history collection; selecting a different applicant in the manager view never substitutes another applicant's residences. Applicants can view and edit applications associated with their accounts. Applicant-information and residence-history saves use separate concurrency tokens, and stale saves are rejected with a reload message. Managers can review applicant names and account emails but cannot edit applicant information.
