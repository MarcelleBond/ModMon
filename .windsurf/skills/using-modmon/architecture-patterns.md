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
// MyApp.Orders/Database/OrdersDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace MyApp.Orders.Database;

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

## Dependency Injection Aggregation Pattern

### Principle
Each module exposes an extension method for service registration. The API project automatically discovers and registers all modules.

### Module-Level Registration
```csharp
// MyApp.Orders/Extensions/DependencyInjection.cs
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

### API-Level Aggregation
```csharp
// MyApp.Api/Extensions/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddProjectModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedKernelDI(configuration);
        // <modules>
        services.AddOrdersDI(configuration);
        services.AddProductsDI(configuration);
        services.AddCustomersDI(configuration);
        // </modules>
        return services;
    }

    public static WebApplication AddProjectDbUpMigrations(
        this WebApplication app,
        IConfiguration configuration)
    {
        // <dbup>
        ThrowIfFailed("Orders", OrdersDbUp.TryMigrate(configuration));
        ThrowIfFailed("Products", ProductsDbUp.TryMigrate(configuration));
        ThrowIfFailed("Customers", CustomersDbUp.TryMigrate(configuration));
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

// MyApp.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProjectModules(builder.Configuration);

var app = builder.Build();

app.AddProjectDbUpMigrations(builder.Configuration);

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
// MyApp.Orders/Database/Scripts/DbUpMigrator.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Configuration;

namespace MyApp.Orders.Database.Scripts;

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

    private static string GetConnectionString(IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("DefaultConnection");
        return conn ?? string.Empty;
    }

    private static UpgradeEngine BuildUpgrader(
        string connectionString)
    {
        var assembly = typeof(DbUpMigrator).Assembly;
        var versioned = GetVersionedScriptNames(assembly);
        var repeatable = GetRepeatableScriptNames(assembly);
        var journalSchema = "orders"; // Module-specific schema
        var journalTable = "schema_versions";

        return DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithTransaction()
            .JournalToPostgresqlTable(journalSchema, journalTable)
            .WithVariablesDisabled()
            .WithScriptsEmbeddedInAssembly(assembly, x => versioned.Contains(x))
            .WithScriptsEmbeddedInAssembly(assembly, x => repeatable.Contains(x))
            .LogToConsole()
            .Build();
    }

    private static HashSet<string> GetVersionedScriptNames(Assembly assembly)
    {
        return assembly.GetManifestResourceNames()
            .Where(x => x.Contains(".Database.Scripts.Versioned."))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> GetRepeatableScriptNames(Assembly assembly)
    {
        return assembly.GetManifestResourceNames()
            .Where(x => x.Contains(".Database.Scripts.Repeatable."))
            .ToHashSet(StringComparer.Ordinal);
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
// MyApp.SharedKernel/Database/Entities/BaseEntity.cs
namespace MyApp.SharedKernel.Database.Entities;

public abstract class BaseEntity
{
    private DateTime? _createdDate;
    private DateTime? _modifiedDate;

    public Guid Id { get; set; }
    public string? CreationUserId { get; set; }
    public string? ModificationUserId { get; set; }

    public DateTime? CreatedDate
    {
        get => _createdDate;
        set => _createdDate = value?.ToUniversalTime();
    }

    public DateTime? ModifiedDate
    {
        get => _modifiedDate;
        set => _modifiedDate = value?.ToUniversalTime();
    }

    protected void EnsureDateTimeKind(ref DateTime? dateTime)
    {
        if (dateTime.HasValue && dateTime.Value.Kind == DateTimeKind.Unspecified)
        {
            dateTime = DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
        }
    }

    protected void EnsureDateTimeKind(ref DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
    }
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
    
    public OrderService(
        OrdersDbContext ordersContext,
        ProductsDbContext productsContext) // ❌ Never inject another module's DbContext!
    {
        _ordersContext = ordersContext;
        _productsContext = productsContext;
    }
    
    public async Task CreateOrder(CreateOrderDto dto)
    {
        // ❌ Accessing another module's database directly
        var product = await _productsContext.Products
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId);
    }
}
```

### Correct Pattern: Interface-Based Communication via SharedKernel

**Step 1: Define interface in SharedKernel**
```csharp
// MyApp.SharedKernel/Interfaces/IProductService.cs
namespace MyApp.SharedKernel.Interfaces;

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

**Step 2: Implement interface in Products module**
```csharp
// MyApp.Products/Services/ProductService.cs
using MyApp.SharedKernel.Interfaces;
using MyApp.Products.Database;

namespace MyApp.Products.Services;

public class ProductService : IProductService
{
    private readonly ProductsDbContext _context;
    
    public ProductService(ProductsDbContext context)
    {
        _context = context;
    }
    
    public async Task<ProductDto> GetProductAsync(Guid productId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);
        
        if (product == null)
            return null;
        
        return new ProductDto(
            product.Id,
            product.Name,
            product.Price,
            product.StockQuantity);
    }
    
    public async Task<bool> IsProductAvailableAsync(Guid productId, int quantity)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);
        
        return product != null && product.StockQuantity >= quantity;
    }
}

// MyApp.Products/Extensions/DependencyInjection.cs
public static IServiceCollection AddProductsDI(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... DbContext registration ...
    
    // ✅ Register implementation of SharedKernel interface
    services.AddScoped<IProductService, ProductService>();
    
    return services;
}
```

**Step 3: Use interface in Orders module**
```csharp
// MyApp.Orders/Services/OrderService.cs
using MyApp.SharedKernel.Interfaces; // ✅ Reference SharedKernel interface
using MyApp.Orders.Database;

namespace MyApp.Orders.Services;

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
        // ✅ Using SharedKernel interface to communicate with Products module
        var product = await _productService.GetProductAsync(dto.ProductId);
        
        if (product == null)
            throw new NotFoundException("Product not found");
        
        var isAvailable = await _productService
            .IsProductAvailableAsync(dto.ProductId, dto.Quantity);
        
        if (!isAvailable)
            throw new BusinessRuleException("Insufficient stock");
        
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

**Important:** Events must be defined in SharedKernel to respect module boundaries.

#### Step 1: Define Event Infrastructure in SharedKernel

```csharp
// MyApp.SharedKernel/Events/IEvent.cs
namespace MyApp.SharedKernel.Events;

public interface IEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

// MyApp.SharedKernel/Events/IEventHandler.cs
public interface IEventHandler<TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

// MyApp.SharedKernel/Events/IEventDispatcher.cs
public interface IEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IEvent;
}

// MyApp.SharedKernel/Events/EventDispatcher.cs
public class EventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public EventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IEvent
    {
        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();
        
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}
```

#### Step 2: Define Domain Events in SharedKernel

```csharp
// ✅ CORRECT: Event in SharedKernel (not in Orders module)
// MyApp.SharedKernel/Events/OrderCreatedEvent.cs
namespace MyApp.SharedKernel.Events;

public class OrderCreatedEvent : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<Guid> ProductIds { get; set; } = new();
}
```

#### Step 3: Register Event Dispatcher in SharedKernel

```csharp
// MyApp.SharedKernel/Extensions/DependencyInjection.cs
namespace MyApp.SharedKernel;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedKernelDI(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register event dispatcher
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        
        return services;
    }
}
```

#### Step 4: Publish Events from Orders Module

```csharp
// MyApp.Orders/Services/OrderService.cs
using MyApp.SharedKernel.Events;
using MyApp.Orders.Database;

namespace MyApp.Orders.Services;

public class OrderService
{
    private readonly OrdersDbContext _context;
    private readonly IEventDispatcher _eventDispatcher;

    public OrderService(
        OrdersDbContext context,
        IEventDispatcher eventDispatcher)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = dto.CustomerId,
            TotalAmount = dto.TotalAmount
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // ✅ Publish event after successful save
        var @event = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            ProductIds = dto.Items.Select(i => i.ProductId).ToList()
        };

        await _eventDispatcher.DispatchAsync(@event);

        return order.Id;
    }
}
```

#### Step 5: Handle Events in Inventory Module

```csharp
// MyApp.Inventory/EventHandlers/OrderCreatedEventHandler.cs
using MyApp.SharedKernel.Events;
using MyApp.Inventory.Database;
using Microsoft.Extensions.Logging;

namespace MyApp.Inventory.EventHandlers;

public class OrderCreatedEventHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(
        InventoryDbContext context,
        ILogger<OrderCreatedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(
        OrderCreatedEvent @event, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Reserving inventory for order {OrderId}", 
            @event.OrderId);

        foreach (var productId in @event.ProductIds)
        {
            var inventory = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);

            if (inventory != null)
            {
                inventory.ReservedQuantity += 1;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

// MyApp.Inventory/Extensions/DependencyInjection.cs
public static IServiceCollection AddInventoryDI(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... DbContext registration ...

    // ✅ Register event handler
    services.AddScoped<IEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();

    return services;
}
```

#### Benefits of This Pattern

**✅ Loose Coupling**
- Inventory module doesn't depend on Orders module
- Orders module doesn't depend on Inventory module
- Both only depend on SharedKernel

**✅ Multiple Handlers**
Multiple modules can react to the same event:

```csharp
// Inventory reserves stock
services.AddScoped<IEventHandler<OrderCreatedEvent>, ReserveInventoryHandler>();

// Notifications sends email
services.AddScoped<IEventHandler<OrderCreatedEvent>, SendOrderConfirmationHandler>();

// Analytics tracks metrics
services.AddScoped<IEventHandler<OrderCreatedEvent>, TrackOrderMetricsHandler>();
```

**✅ Easy Microservice Migration**
When extracting to microservices, replace in-process dispatcher with message bus:

```csharp
// Future: Replace EventDispatcher with RabbitMQ/Azure Service Bus
public class MessageBusEventDispatcher : IEventDispatcher
{
    private readonly IMessageBus _messageBus;
    
    public async Task DispatchAsync<TEvent>(TEvent @event, ...) 
    {
        // Publish to message queue instead of in-process
        await _messageBus.PublishAsync(@event);
    }
}
```

**❌ Anti-Pattern: Events in Module**

```csharp
// ❌ WRONG: Event defined in Orders module
// MyApp.Orders/Events/OrderCreatedEvent.cs
public class OrderCreatedEvent : IEvent { }

// ❌ WRONG: Inventory depends on Orders module
// MyApp.Inventory/EventHandlers/OrderCreatedEventHandler.cs
using MyApp.Orders.Events; // ❌ Creates module dependency!

public class OrderCreatedEventHandler : IEventHandler<OrderCreatedEvent>
{
    // This violates module boundaries
}
```

**Why this is wrong:**
- Creates circular or tight coupling between modules
- Violates module boundaries
- Makes microservice extraction difficult
- Event should be a contract (SharedKernel), not implementation detail
```

## Structured Logging Pattern

### Principle
Use Serilog with structured logging for observability. ModMon configures this automatically.

### Configuration (Auto-Generated)
```csharp
// MyApp.Api/Extensions/SerilogExtensions.cs
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace MyApp.Api.Extensions;

public static class SerilogExtensions
{
    public static void SerilogConfiguration(this ConfigureHostBuilder host)
    {
        host.UseSerilog((_, _, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter());
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
// MyApp.SharedKernel/Middleware/GlobalExceptionHandlingMiddleware.cs
using System.Net;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using MyApp.SharedKernel.Common;
using MyApp.SharedKernel.Exceptions;

namespace MyApp.SharedKernel.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BaseBadRequestException ex)
        {
            await WriteErrorAsync(context, ex.Message, HttpStatusCode.BadRequest);
        }
        catch (BaseNotFoundException ex)
        {
            await WriteErrorAsync(context, ex.Message, HttpStatusCode.NotFound);
        }
        catch (BaseConflictException ex)
        {
            await WriteErrorAsync(context, ex.Message, HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception: {message}", ex.Message);
            await WriteErrorAsync(
                context,
                "We encountered a technical error.",
                HttpStatusCode.InternalServerError);
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        string message,
        HttpStatusCode code)
    {
        var requestId = context.Items[
            Constants.HttpContextKeys.REQUEST_ID_KEY] as Guid?;

        var errorResponse = new GenericExceptionModel
        {
            RequestGuid = requestId ?? Guid.Empty,
            ErrorMessage = message
        };

        var jsonResponse = JsonConvert.SerializeObject(errorResponse);
        context.Response.StatusCode = (int)code;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(jsonResponse);
    }
}
```

## Docker Multi-Stage Build Pattern

### Principle
Use multi-stage builds with chiseled runtime images for minimal attack surface and size.

### Dockerfile (Auto-Generated)
```dockerfile
# BUILD STAGE
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY ["MyApp.Api/MyApp.Api.csproj", "MyApp.Api/"]
COPY ["MyApp.SharedKernel/MyApp.SharedKernel.csproj", "MyApp.SharedKernel/"]
# <modules>
COPY ["MyApp.Orders/MyApp.Orders.csproj", "MyApp.Orders/"]
# </modules>
RUN dotnet restore "MyApp.Api/MyApp.Api.csproj" -a $TARGETARCH

COPY . .
WORKDIR "/src/MyApp.Api"
RUN dotnet publish "MyApp.Api.csproj" \
    -c Release \
    -o /app/publish \
    -a $TARGETARCH \
    --no-restore \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true

# RUNTIME STAGE (Using Chiseled for Security & Size)
# No shell, no apt, no root. Perfect for k3s.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# k3s will handle the user context; Chiseled is non-root by default (UID 1654)
USER app

EXPOSE 8080

# OBSERVABILITY CONFIGURATION
# 1. Structured Logging for Loki
ENV Logging__Console__FormatterName=json
# 2. Globalization invariant mode (Saves space if you don't need culture-specific logic)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
# 3. Ensure OTel/Metrics are enabled
ENV OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_ENABLE_METRICS=true

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
