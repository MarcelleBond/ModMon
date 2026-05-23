# ModMon - .NET Modular Monolith CLI Tool

A powerful CLI tool for rapidly bootstrapping .NET modular monolith applications with clean architecture, PostgreSQL, Docker support, and best practices built-in.

## Features

- ✅ Initialize complete .NET solution with API and SharedKernel projects
- ✅ Add new modules with database context, DI setup, and folder structure
- ✅ Automatic dependency injection aggregation
- ✅ DbUp migration management with versioned and repeatable scripts
- ✅ Docker multi-stage builds with chiseled runtime images
- ✅ Structured logging with Serilog (JSON format for observability)
- ✅ Global exception handling and model validation

## Prerequisites

- .NET SDK 10.0 or later
- `dotnet-ef` tool (for Entity Framework migrations)
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
   dotnet tool install --global ModMon --version 1.0.0
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
- `Dockerfile` and `docker-compose.yml` - Docker configuration

### 2. Add a Module

```bash
modmon module add --name Orders
```

This creates a complete module with:
- Database context and migrations
- Dependency injection setup
- Folder structure for controllers, services, models, etc.
- Automatic integration with the API project

### 3. Run the Application

```bash
# Using .NET CLI
dotnet run --project MyProject.Api

# Using Docker
docker-compose up --build
```

## Commands

### Help

```bash
modmon --help
modmon -h
```

### Initialize Project

```bash
modmon init --name <ProjectName> [--output <Path>]
```

**Options:**
- `--name` (required): Name of the project
- `--output` (optional): Target directory (defaults to current directory)

**Example:**
```bash
modmon init --name ECommerce
modmon init --name ECommerce --output C:\Projects\MyApp
```

### Add Module

```bash
modmon module add --name <ModuleName> [--output <Path>]
```

**Options:**
- `--name` (required): Name of the module
- `--output` (optional): Path to search for .sln file (defaults to current directory)

**Example:**
```bash
modmon module add --name Products
modmon module add --name Customers --output C:\Projects\MyApp
```

### Get Project Information

```bash
modmon info [--path <Path>] [--format json]
```

### Validate Project Structure

```bash
modmon validate [--path <Path>] [--format json]
```

### List Modules

```bash
modmon module list [--path <Path>] [--format json]
```

## Advanced Features

### JSON Output

All commands support `--format json` for machine-readable output:

```bash
modmon init --name MyApp --format json
modmon info --format json
modmon validate --format json
```

### Dry-Run Mode

Preview changes without making them:

```bash
modmon init --name MyApp --dry-run --format json
modmon module add --name Users --dry-run --format json
```

### Verbose Logging

Enable detailed logging:

```bash
modmon init --name MyApp --verbose
modmon module add --name Users --verbose --log-file modmon.log
```

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

The package will be created in `./nupkg/ModMon.1.0.0.nupkg`.

### Install Locally for Testing

```bash
dotnet tool install --global --add-source ./nupkg ModMon
```

### Publish to GitHub Packages

The project includes a GitHub Actions workflow that automatically publishes to GitHub Packages when you create a version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

Or manually trigger the workflow from GitHub Actions with a version number.

## Generated Project Structure

```
MyProject/
├── MyProject.sln
├── Dockerfile
├── docker-compose.yml
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
    │   ├── Entities/
    │   ├── Migrations/
    │   └── Scripts/
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
