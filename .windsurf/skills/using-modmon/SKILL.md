---
name: using-modmon
description: Use when creating .NET modular monolith applications, initializing new projects with clean architecture, adding modules to existing solutions, or working with ModMon CLI tool. Triggers on requests for .NET project scaffolding, modular monolith setup, PostgreSQL integration, Docker configuration, or module-based architecture.
---

# Using ModMon - .NET Modular Monolith CLI

## Overview

ModMon is a CLI tool for rapidly bootstrapping .NET modular monolith applications with clean architecture, PostgreSQL, Docker support, and best practices built-in.

**Core principle:** Accelerate .NET modular monolith development by automating project structure, module creation, and infrastructure setup while enforcing clean architecture patterns.

## When to Use

Invoke this skill when:
- User requests to create a new .NET modular monolith project
- User wants to add a module to an existing ModMon-based solution
- User asks about project structure, validation, or module listing
- User needs help with Docker deployment for modular monoliths
- User mentions "modular monolith", "clean architecture", or "ModMon"
- User wants to scaffold a .NET solution with PostgreSQL and DbUp migrations

## Prerequisites Check

Before using ModMon, verify these requirements:

```bash
# Check .NET SDK version (requires 10.0+)
dotnet --version

# Check if dotnet-ef is installed
dotnet tool list --global | grep dotnet-ef

# Install dotnet-ef if missing
dotnet tool install --global dotnet-ef
```

**Required:**
- .NET SDK 10.0 or later
- `dotnet-ef` tool for Entity Framework migrations
- PostgreSQL (for local development)
- Docker (optional, for containerized deployment)

## Core Commands

### 1. Initialize New Project

**Command:**
```bash
modmon init --name <ProjectName> [--output <Path>]
```

**What it creates:**
- `<ProjectName>.sln` - Solution file
- `<ProjectName>.Api` - ASP.NET Core Web API with Serilog, global exception handling
- `<ProjectName>.SharedKernel` - Shared kernel library with common utilities
- `Dockerfile` and `docker-compose.yml` - Production-ready Docker configuration

**Example:**
```bash
# Create in current directory
modmon init --name ECommerce

# Create in specific directory
modmon init --name ECommerce --output C:\Projects\MyApp

# Preview changes without creating files
modmon init --name ECommerce --dry-run --format json
```

**Generated structure:**
```
ECommerce/
├── ECommerce.sln
├── Dockerfile (multi-stage build with chiseled runtime)
├── docker-compose.yml
├── ECommerce.Api/
│   ├── Program.cs (with Serilog, DI aggregation)
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Extensions/
│       ├── DependencyInjection.cs
│       └── SerilogExtensions.cs
└── ECommerce.SharedKernel/
    ├── Common/
    ├── Database/Entities/
    ├── Exceptions/
    ├── Extensions/
    └── Middleware/
```

### 2. Add Module

**Command:**
```bash
modmon module add --name <ModuleName> [--output <Path>]
```

**What it creates:**
- Complete module with database context
- Entity Framework migrations setup
- DbUp migration management (versioned + repeatable scripts)
- Dependency injection configuration
- Folder structure: Controllers, Services, Models, Interfaces, Exceptions
- Automatic integration with API project

**Example:**
```bash
# Add module in current directory
modmon module add --name Orders

# Add module with specific solution path
modmon module add --name Customers --output C:\Projects\ECommerce

# Preview module structure
modmon module add --name Products --dry-run --format json
```

**Generated module structure:**
```
ECommerce.Orders/
├── AutoMapperProfiles/
├── Controllers/
├── Database/
│   ├── OrdersDbContext.cs
│   ├── Entities/
│   ├── Migrations/ (EF Core migrations)
│   └── Scripts/
│       ├── Versioned/ (one-time migrations)
│       └── Repeatable/ (views, functions, stored procedures)
├── Exceptions/
├── Extensions/
│   └── DependencyInjection.cs (auto-registered in API)
├── Interfaces/
├── Models/
└── Services/
```

### 3. Project Information

**Command:**
```bash
modmon info [--path <Path>] [--format json]
```

**Purpose:** Display project metadata, module count, and configuration

**Example:**
```bash
modmon info
modmon info --path C:\Projects\ECommerce --format json
```

### 4. Validate Project Structure

**Command:**
```bash
modmon validate [--path <Path>] [--format json]
```

**Purpose:** Verify project structure integrity, check for missing files, validate module setup

**Example:**
```bash
modmon validate
modmon validate --format json
```

### 5. List Modules

**Command:**
```bash
modmon module list [--path <Path>] [--format json]
```

**Purpose:** Display all modules in the solution with their status

**Example:**
```bash
modmon module list
modmon module list --format json
```

## Workflow Patterns

### Pattern 1: New Project from Scratch

```bash
# Step 1: Initialize project
modmon init --name MyApp
cd MyApp

# Step 2: Add core modules
modmon module add --name Users
modmon module add --name Products
modmon module add --name Orders

# Step 3: Validate structure
modmon validate

# Step 4: Run application
dotnet run --project MyApp.Api
```

### Pattern 2: Docker Deployment

```bash
# Step 1: Initialize with Docker support (automatic)
modmon init --name MyApp

# Step 2: Add modules as needed
modmon module add --name Inventory

# Step 3: Build and run with Docker
cd MyApp
docker-compose up --build

# Application runs on http://localhost:8080
# PostgreSQL on localhost:5432
```

### Pattern 3: Adding Module to Existing Project

```bash
# Navigate to solution directory
cd C:\Projects\ExistingApp

# Add new module (ModMon auto-detects .sln file)
modmon module add --name Notifications

# Verify integration
modmon validate
modmon module list
```

## Advanced Features

### JSON Output for Automation

All commands support `--format json` for scripting and CI/CD:

```bash
# Get machine-readable output
modmon init --name MyApp --format json
modmon info --format json
modmon validate --format json
modmon module list --format json
```

### Dry-Run Mode

Preview changes before applying:

```bash
modmon init --name MyApp --dry-run --format json
modmon module add --name Users --dry-run --format json
```

### Verbose Logging

Enable detailed logging for troubleshooting:

```bash
modmon init --name MyApp --verbose
modmon module add --name Users --verbose --log-file modmon.log
```

## Architecture Patterns

### Database Context per Module

Each module has its own `DbContext`:

```csharp
// ECommerce.Orders/Database/OrdersDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Orders.Database;

public sealed class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
        : base(options)
    {
    }
    
    // Add your DbSets here
    // public DbSet<Order> Orders { get; set; }
}
```

### Dependency Injection Aggregation

Modules register their services via extension methods:

```csharp
// ECommerce.Orders/Extensions/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Orders.Database;

namespace ECommerce.Orders.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersDI(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration
            .GetConnectionString("DefaultConnection")
            ?? string.Empty;

        services.AddDbContext<OrdersDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        // Add your services here
        // services.AddScoped<IOrderService, OrderService>();
        
        return services;
    }
}
```

```csharp
// ECommerce.Api/Extensions/DependencyInjection.cs
using ECommerce.SharedKernel;
using ECommerce.Orders.Extensions;
using OrdersDbUp = ECommerce.Orders.Database.Scripts.DbUpMigrator;

namespace ECommerce.Api.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddProjectModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedKernelDI(configuration);
        // <modules>
        services.AddOrdersDI(configuration);
        // </modules>
        return services;
    }

    public static WebApplication AddProjectDbUpMigrations(
        this WebApplication app,
        IConfiguration configuration)
    {
        // <dbup>
        ThrowIfFailed("Orders", OrdersDbUp.TryMigrate(configuration));
        // </dbup>
        return app;
    }

    private static void ThrowIfFailed(
        string moduleName,
        int exitCode)
    {
        if (exitCode == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"DbUp migration failed for module '{moduleName}'.");
    }
}
```

API project automatically discovers and registers all modules via the `AddProjectModules` extension method.

### DbUp Migration Management

Each module uses DbUp for database migrations with a `TryMigrate` method:

```csharp
// ECommerce.Orders/Database/Scripts/DbUpMigrator.cs
public static class DbUpMigrator
{
    public static int TryMigrate(
        IConfiguration configuration,
        string? _ = null)
    {
        var connectionString = GetConnectionString(configuration);
        var upgrader = BuildUpgrader(connectionString);
        var result = upgrader.PerformUpgrade();
        return result.Successful ? 0 : 1;
    }
    
    // ... implementation details in architecture-patterns.md
}
```

**Versioned Scripts** (one-time execution):
```
Database/Scripts/Versioned/
├── 001_CreateOrdersTable.sql
├── 002_AddOrderStatusColumn.sql
└── 003_CreateIndexes.sql
```

**Repeatable Scripts** (run on every deployment):
```
Database/Scripts/Repeatable/
├── Views/
│   └── vw_OrderSummary.sql
└── Functions/
    └── fn_CalculateOrderTotal.sql
```

Migrations are executed at application startup via `app.AddProjectDbUpMigrations(configuration)`.

## Common Scenarios

### Scenario 1: E-Commerce Application

```bash
modmon init --name ECommerce
cd ECommerce
modmon module add --name Products
modmon module add --name Orders
modmon module add --name Customers
modmon module add --name Payments
modmon module add --name Inventory
```

### Scenario 2: Multi-Tenant SaaS

```bash
modmon init --name SaaSPlatform
cd SaaSPlatform
modmon module add --name Tenants
modmon module add --name Users
modmon module add --name Billing
modmon module add --name Analytics
```

### Scenario 3: Microservices Migration

Use ModMon to create a modular monolith first, then extract modules to microservices later:

```bash
modmon init --name LegacyMigration
modmon module add --name OrderProcessing
modmon module add --name InventoryManagement
# Each module can later become a microservice
```

## Cross-Module Communication

**Important:** Modules communicate through interfaces defined in SharedKernel, NOT through direct database access.

### ✅ Correct Pattern: SharedKernel Interfaces

**Step 1: Define interface in SharedKernel**
```csharp
// ECommerce.SharedKernel/Interfaces/IProductService.cs
namespace ECommerce.SharedKernel.Interfaces;

public interface IProductService
{
    Task<ProductDto> GetProductAsync(Guid productId);
    Task<bool> IsProductAvailableAsync(Guid productId, int quantity);
}

public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity);
```

**Step 2: Implement in Products module**
```csharp
// ECommerce.Products/Services/ProductService.cs
using ECommerce.SharedKernel.Interfaces;

public class ProductService : IProductService
{
    private readonly ProductsDbContext _context;
    
    // Implementation...
}

// ECommerce.Products/Extensions/DependencyInjection.cs
public static IServiceCollection AddProductsDI(...)
{
    // ... DbContext registration ...
    
    // Register SharedKernel interface implementation
    services.AddScoped<IProductService, ProductService>();
    
    return services;
}
```

**Step 3: Use in Orders module**
```csharp
// ECommerce.Orders/Services/OrderService.cs
using ECommerce.SharedKernel.Interfaces; // ✅ Reference SharedKernel

public class OrderService
{
    private readonly OrdersDbContext _context;
    private readonly IProductService _productService; // ✅ SharedKernel interface
    
    public OrderService(
        OrdersDbContext context,
        IProductService productService)
    {
        _context = context;
        _productService = productService;
    }
    
    public async Task CreateOrder(CreateOrderDto dto)
    {
        // ✅ Use interface to communicate with Products module
        var product = await _productService.GetProductAsync(dto.ProductId);
        // ...
    }
}
```

### ❌ Anti-Pattern: Direct Cross-Module Database Access

```csharp
// ❌ NEVER DO THIS
public class OrderService
{
    private readonly OrdersDbContext _ordersContext;
    private readonly ProductsDbContext _productsContext; // ❌ Wrong!
    
    public OrderService(
        OrdersDbContext ordersContext,
        ProductsDbContext productsContext) // ❌ Never inject another module's DbContext!
    {
        _ordersContext = ordersContext;
        _productsContext = productsContext;
    }
}
```

**Why this is wrong:**
- Violates module boundaries
- Creates tight coupling between modules
- Makes it impossible to extract modules to microservices later
- Breaks the modular monolith pattern

**Always use SharedKernel interfaces for cross-module communication.**

## Red Flags & Common Mistakes

### ❌ Don't: Run commands outside solution directory

```bash
# Wrong - no .sln file found
cd C:\Temp
modmon module add --name Orders
```

**Fix:** Use `--output` parameter or navigate to solution directory:
```bash
modmon module add --name Orders --output C:\Projects\MyApp
```

### ❌ Don't: Manually modify generated DI registration

```bash
# Wrong - manually editing API Program.cs to register modules
services.AddOrdersModule(configuration);
```

**Fix:** ModMon automatically aggregates module DI. Let it handle registration.

### ❌ Don't: Mix EF migrations with DbUp scripts

```bash
# Wrong - using both approaches in same module
dotnet ef migrations add AddOrderTable
# AND creating DbUp script
```

**Fix:** ModMon uses DbUp for migrations. Use `Database/Scripts/Versioned/` for schema changes.

### ❌ Don't: Create modules without proper naming

```bash
# Wrong - invalid characters
modmon module add --name "Order Management"
modmon module add --name Orders&Products
```

**Fix:** Use PascalCase without spaces or special characters:
```bash
modmon module add --name OrderManagement
modmon module add --name Orders
```

### ❌ Don't: Skip validation after adding modules

```bash
modmon module add --name Users
# Immediately start coding without validation
```

**Fix:** Always validate after structural changes:
```bash
modmon module add --name Users
modmon validate
```

### ❌ Don't: Forget to install dotnet-ef tool

```bash
# Wrong - assuming dotnet-ef is installed
modmon init --name MyApp
```

**Fix:** Verify prerequisites first:
```bash
dotnet tool list --global | grep dotnet-ef
dotnet tool install --global dotnet-ef
```

## Troubleshooting

### Issue: "Solution file not found"

**Cause:** Running `module add` outside solution directory

**Fix:**
```bash
# Option 1: Navigate to solution directory
cd C:\Projects\MyApp
modmon module add --name Orders

# Option 2: Specify path
modmon module add --name Orders --output C:\Projects\MyApp
```

### Issue: "Module already exists"

**Cause:** Attempting to add duplicate module

**Fix:**
```bash
# Check existing modules first
modmon module list

# Use different name or delete existing module manually
```

### Issue: Docker build fails

**Cause:** Missing dependencies or incorrect Dockerfile

**Fix:**
```bash
# Validate project structure
modmon validate

# Check Docker logs
docker-compose logs

# Rebuild from scratch
docker-compose down
docker-compose up --build
```

## Integration with Development Workflow

### CI/CD Pipeline Example

```yaml
# .github/workflows/build.yml
- name: Validate ModMon Structure
  run: modmon validate --format json

- name: List Modules
  run: modmon module list --format json

- name: Build Application
  run: dotnet build

- name: Run Tests
  run: dotnet test
```

### Pre-commit Hook Example

```bash
#!/bin/bash
# .git/hooks/pre-commit

# Validate project structure before commit
modmon validate || {
    echo "ModMon validation failed. Fix issues before committing."
    exit 1
}
```

## Best Practices

1. **Always validate after structural changes**
   ```bash
   modmon module add --name NewModule
   modmon validate
   ```

2. **Use dry-run for preview**
   ```bash
   modmon init --name MyApp --dry-run --format json
   ```

3. **Enable verbose logging for troubleshooting**
   ```bash
   modmon module add --name Orders --verbose --log-file modmon.log
   ```

4. **Use JSON output for automation**
   ```bash
   modmon info --format json | jq '.modules | length'
   ```

5. **Follow module naming conventions**
   - Use PascalCase: `OrderManagement`, not `order-management`
   - Be specific: `UserAuthentication`, not `Auth`
   - Avoid plurals in module names: `Order`, not `Orders` (unless domain-appropriate)

6. **Leverage Docker for consistent environments**
   ```bash
   docker-compose up --build
   # Ensures PostgreSQL, API, and all modules run consistently
   ```

7. **Use SharedKernel interfaces for cross-module communication**
   - Define interfaces in `SharedKernel/Interfaces/`
   - Implement in the owning module
   - Reference from consuming modules
   - Never inject another module's DbContext

## Success Criteria

A successful ModMon implementation should:
- ✅ Pass `modmon validate` without errors
- ✅ All modules listed in `modmon module list`
- ✅ Application runs via `dotnet run` or `docker-compose up`
- ✅ Each module has its own DbContext and DI registration (using `Add{Module}DI` pattern)
- ✅ DbUp migrations execute successfully on startup via `AddProjectDbUpMigrations`
- ✅ Serilog structured logging works correctly (RenderedCompactJsonFormatter)
- ✅ Global exception handling catches and logs errors
- ✅ Cross-module communication uses SharedKernel interfaces only
- ✅ No direct cross-module database access

## Related Documentation

- **[ModMon User Guide](../../MODMON_USER_GUIDE.md)** - Comprehensive user documentation
- **[AI Agent Guide](../../AI_AGENT_GUIDE.md)** - Machine-readable documentation for AI agents
- **[README](../../README.md)** - Quick start and installation guide
