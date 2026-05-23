# modmon CLI Tool - User Guide

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Installation & Prerequisites](#installation--prerequisites)
4. [Commands Reference](#commands-reference)
5. [Project Initialization](#project-initialization)
6. [Module Management](#module-management)
7. [Generated Project Structure](#generated-project-structure)
8. [Technical Details](#technical-details)
9. [Workflow Examples](#workflow-examples)
10. [Troubleshooting](#troubleshooting)

---

## Overview

**modmon** is a CLI tool designed to rapidly bootstrap .NET modular monolith applications with a clean architecture approach. It automates the creation of ASP.NET Core Web API projects with:

- **Modular architecture** - Each business domain as a separate module
- **PostgreSQL database** with Entity Framework Core and DbUp migrations
- **Docker support** - Production-ready Dockerfile and docker-compose
- **Shared kernel** - Common middleware, exceptions, and utilities
- **Best practices** - Serilog logging, CORS, Swagger, global exception handling

### Key Features

- ✅ Initialize complete .NET solution with API and SharedKernel projects
- ✅ Add new modules with database context, DI setup, and folder structure
- ✅ Automatic dependency injection aggregation
- ✅ DbUp migration modmoning with versioned and repeatable scripts
- ✅ Docker multi-stage builds with chiseled runtime images
- ✅ Structured logging with Serilog (JSON format for observability)
- ✅ Global exception handling and model validation

---

## Architecture

### Component Overview

```
modmon/
├── Program.cs                    # Entry point
├── Cli/
│   ├── modmonApp.cs           # Command router and orchestrator
│   ├── InitCommandHandler.cs    # Handles 'init' command
│   ├── ModuleAddCommandHandler.cs # Handles 'module add' command
│   ├── Initmodmoner.cs        # Project initialization logic
│   ├── Modulemodmoner.cs      # Module creation logic
│   ├── Templates.cs             # API and Docker templates
│   ├── ModuleTemplates.cs       # Module-specific templates
│   ├── SharedKernelTemplates.cs # Shared kernel code templates
│   ├── DotnetInitSteps.cs       # Dotnet CLI commands for init
│   ├── DotnetModuleSteps.cs     # Dotnet CLI commands for modules
│   ├── EfMigrationSteps.cs      # EF Core migration creation
│   ├── TemplateWriter.cs        # File writing orchestrator
│   ├── ApiDiAggregatorUpdater.cs # Updates DI aggregation file
│   ├── DockerfileModulesUpdater.cs # Updates Dockerfile
│   └── SolutionLocator.cs       # Finds .sln files in directory tree
```

### Design Patterns

- **Command Pattern**: Each command (`init`, `module add`) has dedicated handler
- **Template Method**: modmoning steps follow consistent workflow
- **Dependency Injection**: All components use constructor injection
- **Strategy Pattern**: Different templates for different project types
- **Marker-based Updates**: Uses comment markers for safe file updates

---

## Installation & Prerequisites

### Prerequisites

1. **.NET SDK 10.0** or later
2. **dotnet-ef tool** (for Entity Framework migrations)
   ```bash
   dotnet tool install --global dotnet-ef
   ```
3. **PostgreSQL** (for local development)
4. **Docker** (optional, for containerized deployment)

### Installation from GitHub Packages

#### 1. Configure NuGet for GitHub Packages

Create or update your NuGet configuration file:
- **Windows**: `%APPDATA%\NuGet\NuGet.Config`
- **Linux/macOS**: `~/.nuget/NuGet/NuGet.Config`

Add the following configuration:

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

Replace `YOUR_GITHUB_USERNAME` with your GitHub username and `YOUR_GITHUB_PAT` with a GitHub Personal Access Token that has `read:packages` permission.

#### 2. Install the Tool

```bash
dotnet tool install --global ModMon --version 1.0.0
```

#### 3. Verify Installation

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

### Local Development

#### Building the Tool

```bash
dotnet build
```

#### Running the Tool Locally

```bash
# From the modmon directory
dotnet run -- <command> <options>

# Example
dotnet run -- init --name TestProject
```

#### Pack as NuGet Package

```bash
dotnet pack --configuration Release
```

The package will be created in `./nupkg/ModMon.1.0.0.nupkg`.

#### Install Locally for Testing

```bash
dotnet tool install --global --add-source ./nupkg ModMon
```

---

## Commands Reference

### Help Command

```bash
modmon --help
modmon -h
```

**Output:**
```
Usage:
  modmon init --name <ProjectName> [--output <Path>]
  modmon module add --name <ModuleName> [--output <Path>]
```

### Init Command

**Purpose**: Initialize a new modular monolith project

**Syntax:**
```bash
modmon init --name <ProjectName> [--output <Path>]
```

**Parameters:**
- `--name` (required): Name of the project (e.g., "ECommerce", "InventorySystem")
- `--output` (optional): Target directory (defaults to current directory)

**Example:**
```bash
modmon init --name MyProject
modmon init --name MyProject --output C:\Projects\MyNewApp
```

### Module Add Command

**Purpose**: Add a new module to an existing project

**Syntax:**
```bash
modmon module add --name <ModuleName> [--output <Path>]
```

**Parameters:**
- `--name` (required): Name of the module (e.g., "Orders", "Customers", "Inventory")
- `--output` (optional): Path to search for .sln file (defaults to current directory)

**Example:**
```bash
modmon module add --name Orders
modmon module add --name Customers --output C:\Projects\MyProject
```

---

## Project Initialization

### What Happens During `init`

When you run `modmon init --name MyProject`, the tool executes the following steps:

#### 1. **Solution & Project Creation**

Creates three projects:
- `MyProject.sln` - Solution file
- `MyProject.Api` - ASP.NET Core Web API project
- `MyProject.SharedKernel` - Class library for shared code

#### 2. **NuGet Package Installation**

Automatically adds required packages:
- **API Project**: Swashbuckle.AspNetCore, Serilog.AspNetCore
- **SharedKernel**: Serilog.AspNetCore, Newtonsoft.Json

#### 3. **Project References**

Sets up dependencies:
```
MyProject.Api → MyProject.SharedKernel
```

#### 4. **Template Generation**

Creates the following files:

**API Project (`MyProject.Api/`):**
- `Program.cs` - Application entry point with middleware pipeline
- `appsettings.Development.json` - PostgreSQL connection string
- `Extensions/DependencyInjection.cs` - Module aggregation
- `Extensions/SerilogExtensions.cs` - Logging configuration

**SharedKernel Project (`MyProject.SharedKernel/`):**
- `Database/Entities/BaseEntity.cs` - Base entity with audit fields
- `Common/Constants.cs` - Application constants
- `Exceptions/` - Custom exception types (BadRequest, NotFound, Conflict)
- `Extensions/DateTimeExtensions.cs` - DateTime utilities
- `Middleware/` - Global exception handler, request GUID, model validation
- `Extensions/DependencyInjection.cs` - Shared kernel DI setup

**Root Directory:**
- `Dockerfile` - Multi-stage Docker build with chiseled runtime
- `docker-compose.yml` - API + PostgreSQL orchestration

### Generated API Program.cs Structure

```csharp
// CORS configuration
builder.Services.AddCors(/* AllowAll policy */);

// Serilog structured logging
builder.Host.SerilogConfiguration();

// Controllers with custom model binders
builder.Services.AddControllers(options => {
    options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
    options.Filters.Add<ModelStateValidationFilter>();
});

// Swagger/OpenAPI
builder.Services.AddSwaggerGen();

// Module registration
builder.Services.AddProjectModules(builder.Configuration);

// DbUp migrations
app.AddProjectDbUpMigrations(builder.Configuration);

// Middleware pipeline
app.UseMiddleware<RequestGuidMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
```

---

## Module Management

### What Happens During `module add`

When you run `modmon module add --name Orders`, the tool:

#### 1. **Locates the Solution**

Uses `SolutionLocator` to find the nearest `.sln` file by walking up the directory tree.

#### 2. **Creates Module Project**

```bash
dotnet new classlib -n MyProject.Orders
dotnet sln MyProject.sln add MyProject.Orders/MyProject.Orders.csproj
```

#### 3. **Adds NuGet Packages**

Installs:
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Relational
- Microsoft.EntityFrameworkCore.Design
- Npgsql.EntityFrameworkCore.PostgreSQL
- DbUp
- dbup-postgresql

#### 4. **Sets Up Project References**

```
MyProject.Api → MyProject.Orders
MyProject.Orders → MyProject.SharedKernel
```

#### 5. **Generates Module Structure**

Creates the following folders and files:

```
MyProject.Orders/
├── AutoMapperProfiles/          # (empty, ready for mapping profiles)
├── Controllers/                 # (empty, ready for API controllers)
├── Database/
│   ├── OrdersDbContext.cs       # EF Core DbContext
│   ├── OrdersDbContextFactory.cs # Design-time factory for migrations
│   ├── Entities/                # (empty, ready for entity models)
│   ├── Migrations/              # (EF migrations will go here)
│   └── Scripts/
│       ├── DbUpMigrator.cs      # DbUp migration runner
│       ├── Versioned/
│       │   └── 001_init.sql     # Initial versioned script (placeholder)
│       └── Repeatable/
│           └── R__views.sql     # Repeatable script (placeholder)
├── Exceptions/                  # (empty, ready for module-specific exceptions)
├── Extensions/
│   └── DependencyInjection.cs   # Module DI registration
├── Interfaces/                  # (empty, ready for service interfaces)
├── Models/                      # (empty, ready for DTOs/models)
└── Services/                    # (empty, ready for business logic)
```

#### 6. **Updates API DI Aggregator**

Modifies `MyProject.Api/Extensions/DependencyInjection.cs`:

```csharp
// Adds using statement
using MyProject.Orders.Extensions;
using OrdersDbUp = MyProject.Orders.Database.Scripts.DbUpMigrator;

// Adds module registration
services.AddOrdersDI(configuration);

// Adds DbUp migration call
ThrowIfFailed("Orders", OrdersDbUp.TryMigrate(configuration));
```

#### 7. **Updates Dockerfile**

Adds COPY instruction for the new module:

```dockerfile
# <modules>
COPY ["MyProject.Orders/MyProject.Orders.csproj", "MyProject.Orders/"]
# </modules>
```

#### 8. **Creates Initial EF Migration**

Runs:
```bash
dotnet ef migrations add InitialCreate \
  --output-dir Database/Migrations \
  --project MyProject.Orders \
  --startup-project MyProject.Api
```

---

## Generated Project Structure

### Complete Solution Layout

```
MyProject/
├── MyProject.sln
├── Dockerfile
├── docker-compose.yml
├── MyProject.Api/
│   ├── MyProject.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Extensions/
│       ├── DependencyInjection.cs
│       └── SerilogExtensions.cs
├── MyProject.SharedKernel/
│   ├── MyProject.SharedKernel.csproj
│   ├── Common/
│   │   └── Constants.cs
│   ├── Database/
│   │   └── Entities/
│   │       └── BaseEntity.cs
│   ├── Exceptions/
│   │   ├── GenericExceptionModel.cs
│   │   ├── BaseBadRequestException.cs
│   │   ├── BaseNotFoundException.cs
│   │   └── BaseConflictException.cs
│   ├── Extensions/
│   │   ├── DateTimeExtensions.cs
│   │   └── DependencyInjection.cs
│   └── Middleware/
│       ├── DateTimeModelBinder.cs
│       ├── DateTimeModelBinderProvider.cs
│       ├── GlobalExceptionHandlingMiddleware.cs
│       ├── ModelStateValidationFilter.cs
│       └── RequestGuidMiddleware.cs
└── MyProject.Orders/                    # (example module)
    ├── MyProject.Orders.csproj
    ├── AutoMapperProfiles/
    ├── Controllers/
    ├── Database/
    │   ├── OrdersDbContext.cs
    │   ├── OrdersDbContextFactory.cs
    │   ├── Entities/
    │   ├── Migrations/
    │   └── Scripts/
    │       ├── DbUpMigrator.cs
    │       ├── Versioned/
    │       │   └── 001_init.sql
    │       └── Repeatable/
    │           └── R__views.sql
    ├── Exceptions/
    ├── Extensions/
    │   └── DependencyInjection.cs
    ├── Interfaces/
    ├── Models/
    └── Services/
```

---

## Technical Details

### Database Strategy: Dual Migration System

Each module uses **two migration strategies**:

#### 1. **Entity Framework Core Migrations**
- For schema changes during development
- Type-safe, code-first approach
- Located in `Database/Migrations/`

#### 2. **DbUp Migrations**
- For production deployments
- SQL-based, version-controlled
- Two types:
  - **Versioned** (`Database/Scripts/Versioned/`) - Run once, in order
  - **Repeatable** (`Database/Scripts/Repeatable/`) - Re-run when changed

### DbUp Configuration

Each module's `DbUpMigrator.cs`:
- Reads connection string from configuration
- Embeds SQL scripts as resources
- Uses PostgreSQL journal table: `{module_name}.schema_versions`
- Runs in transaction for safety
- Returns exit code (0 = success, 1 = failure)

### Dependency Injection Pattern

**Module Registration:**
```csharp
public static IServiceCollection AddOrdersDI(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var connectionString = configuration
        .GetConnectionString("DefaultConnection") ?? string.Empty;

    services.AddDbContext<OrdersDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
    });

    // Add module-specific services here

    return services;
}
```

**Aggregation in API:**
```csharp
builder.Services.AddProjectModules(builder.Configuration);
```

### Docker Configuration

**Multi-stage Build:**
1. **Build Stage**: Uses .NET SDK 10.0, restores and publishes
2. **Runtime Stage**: Uses chiseled ASP.NET runtime (minimal, non-root)

**Features:**
- Platform-agnostic builds (`BUILDPLATFORM`, `TARGETARCH`)
- ReadyToRun compilation for faster startup
- Non-root user (UID 1654)
- Structured JSON logging
- Globalization invariant mode (smaller image)

### Middleware Pipeline

**Order of execution:**
1. `RequestGuidMiddleware` - Assigns unique GUID to each request
2. `GlobalExceptionHandlingMiddleware` - Catches and formats exceptions
3. `Authentication` - (if configured)
4. `Authorization` - (if configured)
5. `SerilogRequestLogging` - Logs HTTP requests

### Exception Handling

**Custom Exception Types:**
- `BaseBadRequestException` → HTTP 400
- `BaseNotFoundException` → HTTP 404
- `BaseConflictException` → HTTP 409
- `Exception` (unhandled) → HTTP 500

**Response Format:**
```json
{
  "requestGuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "errorMessage": "Order not found"
}
```

### DateTime Handling

**Automatic UTC Conversion:**
- `DateTimeModelBinder` converts all incoming DateTime to UTC
- `BaseEntity` ensures stored dates are UTC
- `DateTimeExtensions.ToUtc()` helper method

---

## Workflow Examples

### Example 1: Create E-Commerce Application

```bash
# 1. Initialize project
modmon init --name ECommerce --output C:\Projects\ECommerce

# 2. Navigate to project
cd C:\Projects\ECommerce

# 3. Add modules
modmon module add --name Products
modmon module add --name Orders
modmon module add --name Customers
modmon module add --name Payments

# 4. Run locally
dotnet run --project ECommerce.Api

# 5. Or use Docker
docker-compose up --build
```

### Example 2: Add Module to Existing Project

```bash
# Navigate to project root (where .sln is)
cd C:\Projects\MyExistingProject

# Add new module
modmon module add --name Inventory

# The tool will:
# - Find MyExistingProject.sln
# - Create MyExistingProject.Inventory
# - Update DI aggregator
# - Update Dockerfile
# - Create EF migration
```

### Example 3: Development Workflow

```bash
# 1. Create project
modmon init --name TaskManager

# 2. Add module
cd TaskManager
modmon module add --name Tasks

# 3. Add entity to module
# Edit TaskManager.Tasks/Database/Entities/Task.cs

# 4. Create migration
cd TaskManager.Tasks
dotnet ef migrations add AddTaskEntity \
  --startup-project ../TaskManager.Api

# 5. Update database
cd ../TaskManager.Api
dotnet run

# 6. Add controller
# Edit TaskManager.Tasks/Controllers/TasksController.cs

# 7. Test API
curl http://localhost:8080/swagger
```

---

### 3. Get Project Information

Retrieves information about the current project.

```bash
modmon info [--path <Path>] [--format json]
```

**Options:**
- `--path <Path>` (optional) - Project directory (default: current directory)
- `--format json` (optional) - Output as JSON

**Example:**
```bash
modmon info
modmon info --format json
modmon info --path C:\Projects\MyApp
```

### 4. Validate Project Structure

Validates that the project structure is correct.

```bash
modmon validate [--path <Path>] [--format json]
```

**Options:**
- `--path <Path>` (optional) - Project directory (default: current directory)
- `--format json` (optional) - Output as JSON

**Validation Checks:**
- Solution file exists
- API project exists
- SharedKernel project exists
- DI aggregator file exists
- Dockerfile exists

**Example:**
```bash
modmon validate
modmon validate --format json
```

### 5. List Modules

Lists all modules in the project.

```bash
modmon module list [--path <Path>] [--format json]
```

**Options:**
- `--path <Path>` (optional) - Project directory (default: current directory)
- `--format json` (optional) - Output as JSON

**Example:**
```bash
modmon module list
modmon module list --format json
```

## Advanced Features

### Dry-Run Mode

Preview what changes will be made without actually making them:

```bash
modmon init --name MyApp --dry-run --format json
modmon module add --name Users --dry-run --format json
```

In dry-run mode:
- No files are created or modified
- No dotnet commands are executed
- JSON output includes `operations` array showing what would happen
- Exit code is still 0 for successful dry-run

### Verbose Logging

Enable detailed logging for debugging:

```bash
modmon init --name MyApp --verbose
modmon module add --name Users --verbose --log-file modmon.log
```

Logging features:
- `--verbose`: Prints detailed progress to console
- `--log-file <path>`: Writes logs to specified file
- Both flags can be used together
- Log levels: INFO, DEBUG, WARN, ERROR

### JSON Output

Get machine-readable output for automation:

```bash
modmon init --name MyApp --format json
modmon info --format json
modmon validate --format json
```

JSON output includes:
- `success`: Boolean indicating success/failure
- `message`: Human-readable message
- `exitCode`: Numeric exit code (0-8)
- `timestamp`: ISO 8601 UTC timestamp
- Command-specific data fields
- `errors`: Array of error messages (if any)
- `warnings`: Array of warnings (if any)

## Troubleshooting

### Common Issues

#### 1. "dotnet-ef not found"

**Problem**: EF Core tools not installed globally

**Solution:**
```bash
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```

#### 2. "Solution already exists"

**Problem**: Running `init` in directory with existing .sln

**Solution:**
- Use different `--output` path
- Or delete existing solution first

#### 3. "Unable to infer project name from a single *.sln"

**Problem**: Multiple .sln files in directory tree or no .sln found

**Solution:**
- Ensure only one .sln in repository root
- Or use `--output` to specify exact directory

#### 4. "Module already exists"

**Problem**: Module with same name already created

**Solution:**
- Use different module name
- Or manually delete existing module folder and references

#### 5. PostgreSQL Connection Errors

**Problem**: Cannot connect to database

**Solution:**
Check `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=MyProject;Username=MyProject;Password=MyProject-dev"
  }
}
```

Ensure PostgreSQL is running:
```bash
# Using Docker
docker run -d \
  -e POSTGRES_DB=MyProject \
  -e POSTGRES_USER=MyProject \
  -e POSTGRES_PASSWORD=MyProject-dev \
  -p 5432:5432 \
  postgres:16
```

#### 6. Build Errors After Adding Module

**Problem**: Compilation errors in generated code

**Solution:**
- Ensure all NuGet packages restored: `dotnet restore`
- Clean and rebuild: `dotnet clean && dotnet build`
- Check for naming conflicts

---

## Advanced Usage

### Customizing Templates

Templates are defined in:
- `Templates.cs` - API and Docker templates
- `ModuleTemplates.cs` - Module structure
- `SharedKernelTemplates.cs` - Shared kernel code

To customize, modify these files and rebuild the tool.

### Marker-Based Updates

The tool uses comment markers for safe updates:

**In DependencyInjection.cs:**
```csharp
// <modules>
services.AddOrdersDI(configuration);
// </modules>

// <dbup>
ThrowIfFailed("Orders", OrdersDbUp.TryMigrate(configuration));
// </dbup>
```

**In Dockerfile:**
```dockerfile
# <modules>
COPY ["MyProject.Orders/MyProject.Orders.csproj", "MyProject.Orders/"]
# </modules>
```

These markers allow the tool to safely insert new content without breaking existing code.

### Connection String Configuration

**Development:**
- Stored in `appsettings.Development.json`
- Git-ignored by default

**Production:**
- Use environment variables
- Or Azure Key Vault / AWS Secrets Manager
- Or Kubernetes secrets

**Docker Compose:**
```yaml
environment:
  ConnectionStrings__DefaultConnection: "Host=db;Port=5432;..."
```

---

## Best Practices

### 1. Module Organization
- One module per bounded context
- Keep modules loosely coupled
- Use interfaces for cross-module communication

### 2. Database Migrations
- Use EF migrations during development
- Convert to DbUp SQL scripts for production
- Test migrations on staging environment

### 3. Naming Conventions
- Project names: PascalCase (e.g., "ECommerce")
- Module names: PascalCase, singular (e.g., "Order", not "Orders")
- Keep names concise and descriptive

### 4. Security
- Never commit connection strings with real credentials
- Use user secrets for local development
- Implement authentication/authorization as needed

### 5. Testing
- Add test projects for each module
- Use in-memory database for unit tests
- Integration tests with Testcontainers

---

## Summary

The **modmon CLI Tool** provides a rapid, opinionated way to bootstrap .NET modular monolith applications with:

✅ Clean architecture and separation of concerns  
✅ Production-ready Docker configuration  
✅ Dual migration strategy (EF + DbUp)  
✅ Comprehensive middleware pipeline  
✅ Structured logging and observability  
✅ Automatic dependency injection wiring  

**Next Steps:**
1. Initialize your project with `modmon init`
2. Add modules with `modmon module add`
3. Implement your business logic
4. Deploy with Docker or your preferred platform

For issues or contributions, refer to the source code in the `modmon/Cli/` directory.
