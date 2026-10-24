using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.AspNetCore;
using ManualShop.Api.Extensions;
using ManualShop.SharedKernel.Middleware;

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
	option.SwaggerDoc("v1", new() { Title = "ManualShop API" });
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