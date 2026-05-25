# ModMon Architecture Patterns

## Core Architectural Principles

ModMon enforces clean architecture and modular monolith patterns through its scaffolding. Understanding these patterns helps you work effectively with generated code.

## Module Isolation Pattern

### Principle
Each module is a self-contained vertical slice with its own database context, entities, services, and controllers.

### Implementation
```
MyApp.Orders/
├── Database/
│   └── OrdersDbContext.cs          # Module-specific DbContext
├── Entities/
│   ├── Order.cs
│   └── OrderItem.cs
├── Services/
│   └── OrderService.cs
├── Controllers/
│   └── OrdersController.cs
└── Extensions/
    └── DependencyInjection.cs      # Module registration
```

### Benefits
- **Independent deployment**: Modules can be extracted to microservices
- **Team autonomy**: Teams can work on modules independently
- **Clear boundaries**: No cross-module database access
- **Testability**: Each module can be tested in isolation

### Example: Order Module DbContext
```csharp
public class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
        : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
        
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired();
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey(e => e.OrderId);
        });
    }
}
```

## Dependency Injection Aggregation Pattern

### Principle
Each module exposes an extension method for service registration. The API project automatically discovers and registers all modules.

### Module-Level Registration
```csharp
// MyApp.Orders/Extensions/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database context
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Orders"),
                b => b.MigrationsAssembly("MyApp.Orders")));
        
        // Services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        
        // AutoMapper profiles
        services.AddAutoMapper(typeof(OrderProfile));
        
        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();
        
        return services;
    }
}
```

### API-Level Aggregation
```csharp
// MyApp.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// ModMon automatically discovers and registers all module DI extensions
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddProductsModule(builder.Configuration);
builder.Services.AddCustomersModule(builder.Configuration);

var app = builder.Build();
app.Run();
```

## DbUp Migration Management Pattern

### Principle
Use DbUp for database migrations instead of EF Core migrations. This provides better control over versioning and repeatability.

### Directory Structure
```
MyApp.Orders/Database/Scripts/
├── Versioned/                      # One-time migrations (run once)
│   ├── 001_CreateOrdersTable.sql
│   ├── 002_AddOrderStatusColumn.sql
│   └── 003_CreateIndexes.sql
└── Repeatable/                     # Idempotent scripts (run every deployment)
    ├── Views/
    │   └── vw_OrderSummary.sql
    └── Functions/
        └── fn_CalculateOrderTotal.sql
```

### Versioned Migration Example
```sql
-- 001_CreateOrdersTable.sql
CREATE TABLE orders.orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number VARCHAR(50) NOT NULL UNIQUE,
    customer_id UUID NOT NULL,
    order_date TIMESTAMP NOT NULL DEFAULT NOW(),
    total_amount DECIMAL(18,2) NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_orders_customer_id ON orders.orders(customer_id);
CREATE INDEX idx_orders_order_date ON orders.orders(order_date);
```

### Repeatable Script Example
```sql
-- Repeatable/Views/vw_OrderSummary.sql
CREATE OR REPLACE VIEW orders.vw_order_summary AS
SELECT 
    o.id,
    o.order_number,
    o.customer_id,
    o.order_date,
    o.total_amount,
    o.status,
    COUNT(oi.id) AS item_count
FROM orders.orders o
LEFT JOIN orders.order_items oi ON o.id = oi.order_id
GROUP BY o.id, o.order_number, o.customer_id, o.order_date, o.total_amount, o.status;
```

### DbUp Configuration
```csharp
// MyApp.Orders/Database/DatabaseMigrator.cs
public static class DatabaseMigrator
{
    public static void MigrateDatabase(string connectionString)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);
        
        // Versioned scripts
        var versionedUpgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                s => s.Contains(".Database.Scripts.Versioned."))
            .LogToConsole()
            .Build();
        
        var versionedResult = versionedUpgrader.PerformUpgrade();
        
        if (!versionedResult.Successful)
            throw new Exception("Versioned migration failed", versionedResult.Error);
        
        // Repeatable scripts
        var repeatableUpgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                s => s.Contains(".Database.Scripts.Repeatable."))
            .JournalTo(new NullJournal()) // Always run
            .LogToConsole()
            .Build();
        
        var repeatableResult = repeatableUpgrader.PerformUpgrade();
        
        if (!repeatableResult.Successful)
            throw new Exception("Repeatable migration failed", repeatableResult.Error);
    }
}
```

## Shared Kernel Pattern

### Principle
Common functionality shared across all modules lives in SharedKernel. This includes base entities, exceptions, middleware, and utilities.

### SharedKernel Structure
```
MyApp.SharedKernel/
├── Common/
│   ├── BaseEntity.cs               # Base entity with Id, CreatedAt, UpdatedAt
│   ├── Result.cs                   # Result pattern for error handling
│   └── PagedList.cs                # Pagination helper
├── Database/
│   └── Entities/
│       └── AuditableEntity.cs      # Entity with audit fields
├── Exceptions/
│   ├── NotFoundException.cs
│   ├── ValidationException.cs
│   └── BusinessRuleException.cs
├── Extensions/
│   ├── StringExtensions.cs
│   └── DateTimeExtensions.cs
└── Middleware/
    ├── ExceptionHandlingMiddleware.cs
    └── RequestLoggingMiddleware.cs
```

### Base Entity Example
```csharp
// MyApp.SharedKernel/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### Result Pattern Example
```csharp
// MyApp.SharedKernel/Common/Result.cs
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    
    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
    
    public static Result<T> Success(T value) => 
        new Result<T>(true, value, null);
    
    public static Result<T> Failure(string error) => 
        new Result<T>(false, default, error);
}
```

## Cross-Module Communication Pattern

### Principle
Modules communicate through well-defined interfaces and events, never through direct database access.

### Anti-Pattern (Don't Do This)
```csharp
// ❌ BAD: Direct cross-module database access
public class OrderService
{
    private readonly OrdersDbContext _ordersContext;
    private readonly ProductsDbContext _productsContext; // ❌ Wrong!
    
    public async Task CreateOrder(CreateOrderDto dto)
    {
        // ❌ Accessing another module's database directly
        var product = await _productsContext.Products
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId);
    }
}
```

### Correct Pattern: Interface-Based Communication
```csharp
// MyApp.Products/Interfaces/IProductService.cs
public interface IProductService
{
    Task<ProductDto> GetProductAsync(Guid productId);
    Task<bool> IsProductAvailableAsync(Guid productId, int quantity);
}

// MyApp.Orders/Services/OrderService.cs
public class OrderService
{
    private readonly OrdersDbContext _context;
    private readonly IProductService _productService; // ✅ Interface dependency
    
    public OrderService(
        OrdersDbContext context,
        IProductService productService)
    {
        _context = context;
        _productService = productService;
    }
    
    public async Task CreateOrder(CreateOrderDto dto)
    {
        // ✅ Using interface to communicate with Products module
        var product = await _productService.GetProductAsync(dto.ProductId);
        
        if (product == null)
            throw new NotFoundException("Product not found");
        
        var order = new Order
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            TotalAmount = product.Price * dto.Quantity
        };
        
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }
}
```

### Event-Based Communication (Advanced)
```csharp
// MyApp.SharedKernel/Events/IEvent.cs
public interface IEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

// MyApp.Orders/Events/OrderCreatedEvent.cs
public class OrderCreatedEvent : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
}

// MyApp.Inventory/EventHandlers/OrderCreatedEventHandler.cs
public class OrderCreatedEventHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly InventoryDbContext _context;
    
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        // Update inventory based on order creation
        // This keeps modules decoupled
    }
}
```

## Structured Logging Pattern

### Principle
Use Serilog with structured logging for observability. ModMon configures this automatically.

### Configuration (Auto-Generated)
```csharp
// MyApp.Api/Extensions/SerilogExtensions.cs
public static class SerilogExtensions
{
    public static IHostBuilder UseSerilogLogging(this IHostBuilder host)
    {
        return host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.File(
                    new JsonFormatter(),
                    "logs/log-.txt",
                    rollingInterval: RollingInterval.Day);
        });
    }
}
```

### Usage in Services
```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
    {
        _logger.LogInformation(
            "Creating order for customer {CustomerId} with {ItemCount} items",
            dto.CustomerId,
            dto.Items.Count);
        
        try
        {
            var order = await CreateOrderInternalAsync(dto);
            
            _logger.LogInformation(
                "Order {OrderId} created successfully with total {TotalAmount}",
                order.Id,
                order.TotalAmount);
            
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create order for customer {CustomerId}",
                dto.CustomerId);
            throw;
        }
    }
}
```

## Global Exception Handling Pattern

### Principle
Centralized exception handling in middleware ensures consistent error responses.

### Implementation (Auto-Generated)
```csharp
// MyApp.SharedKernel/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status404NotFound);
        }
        catch (ValidationException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest);
        }
        catch (BusinessRuleException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status422UnprocessableEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError);
        }
    }
    
    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        int statusCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        
        var response = new
        {
            error = exception.Message,
            statusCode = statusCode,
            timestamp = DateTime.UtcNow
        };
        
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

## Docker Multi-Stage Build Pattern

### Principle
Use multi-stage builds with chiseled runtime images for minimal attack surface and size.

### Dockerfile (Auto-Generated)
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MyApp.Api/MyApp.Api.csproj", "MyApp.Api/"]
COPY ["MyApp.SharedKernel/MyApp.SharedKernel.csproj", "MyApp.SharedKernel/"]
COPY ["MyApp.Orders/MyApp.Orders.csproj", "MyApp.Orders/"]

RUN dotnet restore "MyApp.Api/MyApp.Api.csproj"

COPY . .
WORKDIR "/src/MyApp.Api"
RUN dotnet build "MyApp.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Api.csproj" -c Release -o /app/publish

# Runtime stage (chiseled image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-chiseled AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Api.dll"]
```

### Benefits
- **Small image size**: Chiseled images are ~100MB vs ~200MB for standard runtime
- **Security**: No shell, package manager, or unnecessary tools
- **Fast builds**: Layer caching optimizes rebuild times
- **Production-ready**: Includes health checks and proper signal handling

## Summary

ModMon enforces these architectural patterns:
1. **Module Isolation**: Each module is self-contained with its own DbContext
2. **DI Aggregation**: Automatic service registration via extension methods
3. **DbUp Migrations**: Versioned and repeatable database scripts
4. **Shared Kernel**: Common functionality in a shared library
5. **Interface-Based Communication**: No direct cross-module database access
6. **Structured Logging**: Serilog with JSON output for observability
7. **Global Exception Handling**: Centralized error handling middleware
8. **Docker Multi-Stage Builds**: Minimal, secure runtime images

These patterns enable:
- Easy extraction of modules to microservices
- Team autonomy and parallel development
- Consistent error handling and logging
- Production-ready deployments
- Maintainable, testable code
