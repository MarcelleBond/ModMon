# ModMon Quick Reference

## Command Cheat Sheet

### Initialization
```bash
modmon init --name <ProjectName>                    # Create in current directory
modmon init --name <ProjectName> --output <Path>    # Create in specific directory
modmon init --name <ProjectName> --dry-run          # Preview without creating
modmon init --name <ProjectName> --verbose          # Detailed logging
```

### Module Management
```bash
modmon module add --name <ModuleName>               # Add module to current solution
modmon module add --name <ModuleName> --output <Path>  # Specify solution path
modmon module list                                  # List all modules
modmon module list --format json                    # JSON output
```

### Project Operations
```bash
modmon info                                         # Show project information
modmon info --format json                           # JSON output
modmon validate                                     # Validate project structure
modmon validate --format json                       # JSON validation output
```

### Global Options
```bash
--format json                                       # Machine-readable output
--dry-run                                          # Preview changes only
--verbose                                          # Enable detailed logging
--log-file <path>                                  # Save logs to file
--help, -h                                         # Show help information
```

## Common Command Combinations

### New Project Setup
```bash
modmon init --name MyApp && cd MyApp && modmon module add --name Users && modmon validate
```

### Batch Module Creation
```bash
cd MyProject
modmon module add --name Users
modmon module add --name Products
modmon module add --name Orders
modmon validate
```

### CI/CD Validation
```bash
modmon validate --format json && modmon module list --format json
```

## File Structure Quick Reference

### After `modmon init --name MyApp`
```
MyApp/
├── MyApp.sln
├── Dockerfile
├── docker-compose.yml
├── MyApp.Api/
│   ├── Program.cs
│   ├── appsettings.json
│   └── Extensions/
└── MyApp.SharedKernel/
    ├── Common/
    ├── Database/
    ├── Exceptions/
    ├── Extensions/
    └── Middleware/
```

### After `modmon module add --name Orders`
```
MyApp.Orders/
├── AutoMapperProfiles/
├── Controllers/
├── Database/
│   ├── OrdersDbContext.cs
│   ├── Entities/
│   ├── Migrations/
│   └── Scripts/
│       ├── Versioned/      # One-time migrations
│       └── Repeatable/     # Views, functions, stored procedures
├── Exceptions/
├── Extensions/
│   └── DependencyInjection.cs
├── Interfaces/
├── Models/
└── Services/
```

## Error Messages & Solutions

### "Solution file not found"
**Solution:** Navigate to solution directory or use `--output` parameter
```bash
cd C:\Projects\MyApp
# OR
modmon module add --name Orders --output C:\Projects\MyApp
```

### "Module already exists"
**Solution:** Check existing modules and use different name
```bash
modmon module list
```

### "dotnet-ef tool not found"
**Solution:** Install Entity Framework tools
```bash
dotnet tool install --global dotnet-ef
```

### "Invalid project name"
**Solution:** Use PascalCase without spaces or special characters
```bash
# Wrong: "My App", "My-App", "my_app"
# Correct: "MyApp", "ECommerce", "OrderManagement"
```

## Docker Quick Commands

```bash
# Build and run
docker-compose up --build

# Run in background
docker-compose up -d

# View logs
docker-compose logs -f

# Stop containers
docker-compose down

# Rebuild from scratch
docker-compose down -v && docker-compose up --build
```

## Database Connection Strings

### Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MyApp;Username=postgres;Password=postgres"
  }
}
```

### Docker (docker-compose.yml)
```yaml
environment:
  - ConnectionStrings__DefaultConnection=Host=postgres;Database=MyApp;Username=postgres;Password=postgres
```

## Module Naming Conventions

### ✅ Good Names
- `Users`
- `Products`
- `OrderManagement`
- `PaymentProcessing`
- `InventoryTracking`

### ❌ Bad Names
- `User Management` (spaces)
- `orders&products` (special characters)
- `my-module` (hyphens in module names)
- `Module1` (non-descriptive)

## Typical Workflows

### E-Commerce Application
```bash
modmon init --name ECommerce
cd ECommerce
modmon module add --name Products
modmon module add --name Orders
modmon module add --name Customers
modmon module add --name Payments
modmon module add --name Inventory
modmon validate
docker-compose up --build
```

### SaaS Platform
```bash
modmon init --name SaaSPlatform
cd SaaSPlatform
modmon module add --name Tenants
modmon module add --name Users
modmon module add --name Billing
modmon module add --name Analytics
modmon validate
dotnet run --project SaaSPlatform.Api
```

### Microservices Preparation
```bash
modmon init --name ModularMonolith
cd ModularMonolith
modmon module add --name OrderProcessing
modmon module add --name InventoryManagement
modmon module add --name CustomerService
# Each module can later be extracted to a microservice
```

## JSON Output Examples

### Project Info
```bash
modmon info --format json
```
```json
{
  "projectName": "MyApp",
  "modules": ["Users", "Products", "Orders"],
  "moduleCount": 3,
  "solutionPath": "C:\\Projects\\MyApp\\MyApp.sln"
}
```

### Validation
```bash
modmon validate --format json
```
```json
{
  "isValid": true,
  "errors": [],
  "warnings": [],
  "checkedItems": [
    "Solution file exists",
    "API project exists",
    "SharedKernel project exists",
    "All modules have DbContext",
    "All modules have DI registration"
  ]
}
```

### Module List
```bash
modmon module list --format json
```
```json
{
  "modules": [
    {
      "name": "Users",
      "path": "C:\\Projects\\MyApp\\MyApp.Users",
      "hasDbContext": true,
      "hasDependencyInjection": true
    },
    {
      "name": "Products",
      "path": "C:\\Projects\\MyApp\\MyApp.Products",
      "hasDbContext": true,
      "hasDependencyInjection": true
    }
  ]
}
```

## Prerequisites Checklist

Before using ModMon, ensure:
- [ ] .NET SDK 10.0+ installed (`dotnet --version`)
- [ ] dotnet-ef tool installed (`dotnet tool list --global`)
- [ ] PostgreSQL running (for local development)
- [ ] Docker installed (optional, for containerized deployment)

## Performance Tips

1. **Use dry-run for large operations**
   ```bash
   modmon init --name LargeApp --dry-run --format json
   ```

2. **Enable verbose logging only when troubleshooting**
   ```bash
   modmon module add --name Orders --verbose --log-file debug.log
   ```

3. **Use JSON output for scripting**
   ```bash
   MODULES=$(modmon module list --format json | jq -r '.modules[].name')
   ```

4. **Validate before committing**
   ```bash
   modmon validate || exit 1
   ```
