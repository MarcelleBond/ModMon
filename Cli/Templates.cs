using System.Text;

namespace ModMon.Cli;

internal static class Templates
{
	public static string ApiProgram(string projectName)
	{
		return $$""" 
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.AspNetCore;
using {{projectName}}.Api.Extensions;
using {{projectName}}.SharedKernel.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy
			.AllowAnyOrigin()
			.AllowAnyHeader()
			.AllowAnyMethod()
			.WithExposedHeaders("Content-Disposition");
	});
});

builder.Host.SerilogConfiguration();

builder.Services.AddControllers(options =>
{
	options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
	options.Filters.Add<ModelStateValidationFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
	option.SwaggerDoc("v1", new() { Title = "{{projectName}} API" });
});

builder.Services.AddProjectModules(builder.Configuration);

var app = builder.Build();

app.AddProjectDbUpMigrations(builder.Configuration);

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseMiddleware<RequestGuidMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();

app.MapControllers();
app.Run();
""";
	}

	public static string ApiAppSettingsDevelopment(string projectName)
	{
		return $$"""
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database={{projectName}};Username={{projectName}};Password={{projectName}}-dev"
  }
}
""";
	}

	public static string ApiModulesAggregator(
		string projectName,
		IReadOnlyCollection<string> modules)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"using {projectName}.SharedKernel;");
		foreach (var module in modules)
		{
			sb.AppendLine($"using {projectName}.{module}.Extensions;");
			sb.AppendLine(
				$"using {module}DbUp = {projectName}.{module}.Database.Scripts." +
				"DbUpMigrator;");
		}

		sb.AppendLine();
		sb.AppendLine($"namespace {projectName}.Api.Extensions;");
		sb.AppendLine();
		sb.AppendLine("public static class DependencyInjection");
		sb.AppendLine("{");
		sb.AppendLine("\tpublic static IServiceCollection AddProjectModules(");
		sb.AppendLine("\t\tthis IServiceCollection services,");
		sb.AppendLine("\t\tIConfiguration configuration)");
		sb.AppendLine("\t{");
		sb.AppendLine("\t\tservices.AddSharedKernelDI(configuration);");
		sb.AppendLine("\t\t// <modules>");
		foreach (var module in modules)
		{
			sb.AppendLine($"\t\tservices.Add{module}DI(configuration);");
		}
		sb.AppendLine("\t\t// </modules>");
		sb.AppendLine("\t\treturn services;");
		sb.AppendLine("\t}");
		sb.AppendLine();
		sb.AppendLine("\tpublic static WebApplication AddProjectDbUpMigrations(");
		sb.AppendLine("\t\tthis WebApplication app,");
		sb.AppendLine("\t\tIConfiguration configuration)");
		sb.AppendLine("\t{");
		sb.AppendLine("\t\t// <dbup>");
		foreach (var module in modules)
		{
			sb.AppendLine(
				$"\t\tThrowIfFailed(\"{module}\", " +
				$"{module}DbUp.TryMigrate(configuration));");
		}
		sb.AppendLine("\t\t// </dbup>");
		sb.AppendLine("\t\treturn app;");
		sb.AppendLine("\t}");
		sb.AppendLine();
		sb.AppendLine("\tprivate static void ThrowIfFailed(");
		sb.AppendLine("\t\tstring moduleName,");
		sb.AppendLine("\t\tint exitCode)");
		sb.AppendLine("\t{");
		sb.AppendLine("\t\tif (exitCode == 0)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\treturn;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tthrow new InvalidOperationException(");
		sb.AppendLine("\t\t\t$\"DbUp migration failed for module '{moduleName}'.\");");
		sb.AppendLine("\t}");
		sb.AppendLine("}");
		return sb.ToString();
	}

	public static string RootDockerfile(string projectName)
	{
		return $$"""
# BUILD STAGE
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY ["{{projectName}}.Api/{{projectName}}.Api.csproj", "{{projectName}}.Api/"]
COPY ["{{projectName}}.SharedKernel/{{projectName}}.SharedKernel.csproj", "{{projectName}}.SharedKernel/"]
# <modules>
# </modules>
RUN dotnet restore "{{projectName}}.Api/{{projectName}}.Api.csproj" -a $TARGETARCH

COPY . .
WORKDIR "/src/{{projectName}}.Api"
RUN dotnet publish "{{projectName}}.Api.csproj" \
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

ENTRYPOINT ["dotnet", "{{projectName}}.Api.dll"]
""";
	}

	public static string DockerCompose(string projectName)
	{
		return $$"""
services:
  api:
    image: {{projectName}}-api:dev
    container_name: {{projectName}}-api
    build:
      context: .
      dockerfile: Dockerfile
    depends_on:
      - db
    environment:
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database={{projectName}};Username={{projectName}};Password={{projectName}}-dev
    ports:
      - "8080:8080"

  db:
    image: postgres:16
    container_name: {{projectName}}-db
    restart: unless-stopped
    environment:
      POSTGRES_DB: {{projectName}}
      POSTGRES_USER: {{projectName}}
      POSTGRES_PASSWORD: {{projectName}}-dev
    ports:
      - "5432:5432"
    volumes:
      - {{projectName}}_pgdata:/var/lib/postgresql/data

volumes:
  {{projectName}}_pgdata:
""";
	}
}
