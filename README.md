# ModMon - .NET Modular Monolith CLI Tool

A powerful CLI tool for rapidly bootstrapping .NET modular monolith applications with clean architecture, PostgreSQL, Docker support, and best practices built-in.

## Features

- ✅ Initialize complete .NET solution with API, SharedKernel, `.gitignore`, and AI-agent guidance
- ✅ Add new modules with database context, DI setup, and folder structure
- ✅ Automatic dependency injection aggregation
- ✅ Dual database migrations — EF Core schema migrations applied at startup, then DbUp versioned and repeatable scripts
- ✅ Docker multi-stage builds with chiseled runtime images
- ✅ Structured logging with Serilog (JSON format for observability)
- ✅ Global exception handling and model validation
- ✅ Dry-run preview of all filesystem and `dotnet` operations
- ✅ Machine-readable `--format json` (stdout stays pure JSON; progress goes to stderr)
- ✅ Automatic rollback when scaffolding fails partway
- ✅ Architecture validation of the project-reference graph

## Prerequisites

- .NET SDK 10.0 or later
- `dotnet-ef` tool — recommended for Entity Framework migrations during
  `module add` (optional: without it the module is still created and a
  warning is emitted)
  ```bash
  dotnet tool install --global dotnet-ef
  ```
- PostgreSQL (for local development)
- Docker (optional, for containerized deployment)

## Installation

### Install from GitHub Packages

1. **Configure NuGet to use GitHub Packages**

   Create or update `%APPDATA%\NuGet\NuGet.Config` (Windows) or `~/.nuget/NuGet/NuGet.Config` (Linux/macOS):

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
       <add key="github-modmon" value="https://nuget.pkg.github.com/MarcelleBond/index.json" />
     </packageSources>
     <packageSourceCredentials>
       <github-modmon>
         <add key="Username" value="YOUR_GITHUB_USERNAME" />
         <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
       </github-modmon>
     </packageSourceCredentials>
   </configuration>
   ```

   Replace `YOUR_GITHUB_USERNAME` and `YOUR_GITHUB_PAT` with your GitHub username and Personal Access Token (with `read:packages` permission).

2. **Install the tool globally**

   ```bash
   dotnet tool install --global ModMon --version 1.1.0
   ```

3. **Verify installation**

   ```bash
   modmon --help
   ```

### Update the Tool

```bash
dotnet tool update --global ModMon
```

### Uninstall the Tool

```bash
dotnet tool uninstall --global ModMon
```

## Quick Start

### 1. Initialize a New Project

```bash
modmon init --name MyProject
cd MyProject
```

This creates:
- `MyProject.sln` - Solution file
- `MyProject.Api` - ASP.NET Core Web API project
- `MyProject.SharedKernel` - Shared kernel library
- `Dockerfile`, `docker-compose.yml`, `.dockerignore` - Docker setup
  (Postgres with a healthcheck; the API waits for the database)
- `.gitignore` - standard .NET ignore rules (preserved if one exists)
- `AGENTS.md` + `.agents/skills/modmon/` - AI-agent guidance files

### 2. Add a Module

```bash
modmon module add --name Orders
```

This creates a complete module with:
- Database context and EF Core migrations (`dotnet ef`)
- Dependency injection setup
- Folder structure for controllers, services, models, etc.
- Automatic integration with the API project — DI registration plus EF
  Core and DbUp migration calls wired into the API aggregator
- A `Microsoft.AspNetCore.App` framework reference so controllers are
  discovered by the API host

### 3. Run the Application

```bash
# Using .NET CLI
dotnet run --project MyProject.Api

# Using Docker
docker compose up --build
```

## Commands

### Help

```bash
modmon --help
modmon -h
```

### Initialize Project

```bash
modmon init --name <ProjectName> [--output <Path>] [--format json] [--dry-run]
```

**Options:**
- `--name` (required): Name of the project. Letters, digits and
  underscores; must start with a letter.
- `--output` (optional): Target directory (defaults to current directory)
- `--format json` (optional): Machine-readable output
- `--dry-run` (optional): Preview all changes without writing anything

`init` also generates project-specific AI guidance in `AGENTS.md` and
`.agents/skills/modmon/`. ModMon-owned guidance files may be regenerated; existing
unmarked user files are preserved.

**Example:**
```bash
modmon init --name ECommerce
modmon init --name ECommerce --output C:\Projects\MyApp
```

### Add Module

```bash
modmon module add --name <ModuleName> [--output <Path>] [--format json] [--dry-run]
```

**Options:**
- `--name` (required): Name of the module (same naming rules as `init`)
- `--output` (optional): Path to search for .sln file (defaults to current directory)
- `--format json` (optional): Machine-readable output
- `--dry-run` (optional): Preview all changes without writing anything

If the module cannot be fully wired in (e.g. the API DI aggregator or
`Dockerfile` is missing or lacks the expected markers), the module is
still created and the result includes warnings telling you what was
skipped.

**Example:**
```bash
modmon module add --name Products
modmon module add --name Customers --output C:\Projects\MyApp
```

### Get Project Information

```bash
modmon info [--path <Path>] [--format json]
```

### Validate Project Structure and Boundaries

```bash
modmon validate [--path <Path>] [--format json]
```

Validation checks required project files and enforces project-reference
boundaries: the API may reference only modules and SharedKernel; modules may
reference only SharedKernel, not one another. Unresolved project-reference
configuration fails validation rather than being assumed valid. This checks
the project graph only; it does not analyze C# source usage. Boundary or
unverifiable-reference failures return exit code 5.

### List Modules

```bash
modmon module list [--path <Path>] [--format json]
```

## Advanced Features

### Database Migrations

Generated projects apply two migration systems at startup, in a fixed
order via `ApplyProjectMigrations` in the API's `DependencyInjection.cs`:

1. **EF Core migrations** (`// <ef>` section) own entity schema —
   tables, columns, and indexes from each module's DbContext.
2. **DbUp scripts** (`// <dbup>` section) own supplemental objects —
   views, functions, and seed data under `Database/Scripts/`.

`module add` wires each new module into both marker sections. Keep
ownership separate: never manage the same object in both systems.

### JSON Output

All commands support `--format json` for machine-readable output. Stdout
emits exactly one JSON document; progress messages and `dotnet` output
are routed to stderr so the output is safe to pipe:

```bash
modmon init --name MyApp --format json
modmon info --format json
modmon validate --format json
```

### Dry-Run Mode

Preview changes without making them. Nothing is written to disk and no
`dotnet` commands run; the result lists every operation that *would*
happen (file writes, package installs, EF migrations):

```bash
modmon init --name MyApp --dry-run
modmon module add --name Users --dry-run --format json
```

### Verbose Logging & Log Files

Enable detailed logging to the console, a file, or both:

```bash
modmon init --name MyApp --verbose
modmon module add --name Users --verbose --log-file modmon.log
modmon validate --log-file logs\audit.log
```

`--log-file` creates missing parent directories and appends
timestamped entries.

### Failure Rollback

If `init` or `module add` fails partway (e.g. a `dotnet` step errors or a
template write fails), ModMon rolls back what it created: new module
directories are removed, solution/project references are undone, and
previously-modified files (`DependencyInjection.cs`, `Dockerfile`) are
restored. Pre-existing files are never deleted.

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General / unexpected failure |
| 2 | Invalid arguments (bad or missing options, invalid name) |
| 3 | Resource already exists (solution or module) |
| 4 | Resource not found (no solution discovered) |
| 5 | Validation failed |
| 6 | External tool error (`dotnet` step failed) |

## Documentation

- **[User Guide](MODMON_USER_GUIDE.md)** - Comprehensive user documentation
- **[AI Agent Guide](AI_AGENT_GUIDE.md)** - Machine-readable documentation for AI agents

## Development & Deployment

### Build Locally

```bash
dotnet build
dotnet run -- init --name TestProject
```

### Pack as NuGet Package

```bash
dotnet pack --configuration Release
```

The package will be created in `./nupkg/ModMon.1.1.0.nupkg`.

### Install Locally for Testing

```bash
dotnet tool install --global --add-source ./nupkg ModMon
```

### Publish to GitHub Packages

The project includes a GitHub Actions workflow that automatically publishes to GitHub Packages when you create a version tag:

```bash
git tag v1.1.0
git push origin v1.1.0
```

Or manually trigger the workflow from GitHub Actions with a version number.

## Generated Project Structure

```
MyProject/
├── MyProject.sln
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── .gitignore
├── AGENTS.md
├── .agents/skills/modmon/
│   ├── SKILL.md
│   ├── architecture-patterns.md
│   └── quick-reference.md
├── MyProject.Api/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Extensions/
│       ├── DependencyInjection.cs
│       └── SerilogExtensions.cs
├── MyProject.SharedKernel/
│   ├── Common/
│   ├── Database/Entities/
│   ├── Exceptions/
│   ├── Extensions/
│   └── Middleware/
└── MyProject.Orders/
    ├── AutoMapperProfiles/
    ├── Controllers/
    ├── Database/
    │   ├── OrdersDbContext.cs
    │   ├── OrdersDbContextFactory.cs
    │   ├── Entities/
    │   ├── Migrations/
    │   └── Scripts/
    │       ├── DbUpMigrator.cs
    │       ├── Versioned/001_init.sql
    │       └── Repeatable/R__views.sql
    ├── Exceptions/
    ├── Extensions/
    ├── Interfaces/
    ├── Models/
    └── Services/
```

## License

MIT

## Repository

https://github.com/MarcelleBond/ModMon
