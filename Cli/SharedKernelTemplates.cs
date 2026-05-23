namespace ModMon.Cli;

internal static class SharedKernelTemplates
{
	public static void WriteAll(
		IFileSystemWriter fileSystem,
		string kernelRoot,
		string projectName)
	{
		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Database", "Entities", "BaseEntity.cs"),
			KernelTemplates.BaseEntity(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Common", "Constants.cs"),
			KernelTemplates.Constants(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Exceptions", "GenericExceptionModel.cs"),
			KernelTemplates.GenericExceptionModel(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Exceptions", "BaseBadRequestException.cs"),
			KernelTemplates.BaseBadRequestException(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Exceptions", "BaseNotFoundException.cs"),
			KernelTemplates.BaseNotFoundException(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Exceptions", "BaseConflictException.cs"),
			KernelTemplates.BaseConflictException(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Extensions", "DateTimeExtensions.cs"),
			KernelTemplates.DateTimeExtensions(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Middleware", "DateTimeModelBinder.cs"),
			KernelTemplates.DateTimeModelBinder(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Middleware", "DateTimeModelBinderProvider.cs"),
			KernelTemplates.DateTimeModelBinderProvider(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Middleware", "RequestGuidMiddleware.cs"),
			KernelTemplates.RequestGuidMiddleware(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Middleware", "GlobalExceptionHandlingMiddleware.cs"),
			KernelTemplates.GlobalExceptionHandlingMiddleware(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Middleware", "ModelStateValidationFilter.cs"),
			KernelTemplates.ModelStateValidationFilter(projectName));

		WriteFile(
			fileSystem,
			kernelRoot,
			Path.Combine("Extensions", "DependencyInjection.cs"),
			KernelTemplates.DependencyInjection(projectName));
	}

	private static void WriteFile(
		IFileSystemWriter fileSystem,
		string kernelRoot,
		string relativePath,
		string content)
	{
		fileSystem.WriteFile(
			Path.Combine(kernelRoot, relativePath),
			content,
			overwrite: true);
	}

	private static class KernelTemplates
	{
		public static string BaseEntity(string projectName)
		{
			return $$"""
namespace {{projectName}}.SharedKernel.Database.Entities;

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
""";
		}

		public static string Constants(string projectName)
		{
			return $$"""
namespace {{projectName}}.SharedKernel.Common;

public static class Constants
{
	public static class HttpContextKeys
	{
		public const string REQUEST_ID_KEY = "RequestId";
	}
}
""";
		}

		public static string GenericExceptionModel(string projectName)
		{
			return $$"""
namespace {{projectName}}.SharedKernel.Exceptions;

public sealed class GenericExceptionModel
{
	public Guid RequestGuid { get; set; }
	public required string ErrorMessage { get; set; }
}
""";
		}

		public static string BaseBadRequestException(string projectName)
		{
			return $$"""
namespace {{projectName}}.SharedKernel.Exceptions;

public abstract class BaseBadRequestException : Exception
{
	protected BaseBadRequestException()
	{
	}

	protected BaseBadRequestException(string message)
		: base(message)
	{
	}
}
""";
		}

		public static string BaseNotFoundException(string projectName)
		{
			return $$"""
namespace {{projectName}}.SharedKernel.Exceptions;

public abstract class BaseNotFoundException : Exception
{
	protected BaseNotFoundException()
	{
	}

	protected BaseNotFoundException(string message)
		: base(message)
	{
	}
}
""";
		}

		public static string BaseConflictException(string projectName)
		{
			return $$"""
namespace {{projectName}}.SharedKernel.Exceptions;

public class BaseConflictException : Exception
{
	public BaseConflictException()
	{
	}

	public BaseConflictException(string message)
		: base(message)
	{
	}
}
""";
		}

		public static string DateTimeExtensions(string projectName)
		{
			return $$"""
namespace {{projectName}}.SharedKernel.Extensions;

public static class DateTimeExtensions
{
	public static DateTime ToUtc(this DateTime value)
	{
		return value.Kind == DateTimeKind.Utc
			? value
			: DateTime.SpecifyKind(value, DateTimeKind.Utc);
	}
}
""";
		}

		public static string DateTimeModelBinder(string projectName)
		{
			return $$"""
using Microsoft.AspNetCore.Mvc.ModelBinding;
using {{projectName}}.SharedKernel.Extensions;

namespace {{projectName}}.SharedKernel.Middleware;

public sealed class DateTimeModelBinder : IModelBinder
{
	public Task BindModelAsync(ModelBindingContext bindingContext)
	{
		if (bindingContext == null)
		{
			throw new ArgumentNullException(nameof(bindingContext));
		}

		var valueProviderResult = bindingContext.ValueProvider.GetValue(
			bindingContext.ModelName);
		if (valueProviderResult == ValueProviderResult.None)
		{
			return Task.CompletedTask;
		}

		bindingContext.ModelState.SetModelValue(
			bindingContext.ModelName,
			valueProviderResult);

		var value = valueProviderResult.FirstValue;
		if (string.IsNullOrEmpty(value))
		{
			return Task.CompletedTask;
		}

		if (!DateTime.TryParse(value, out var dateTime))
		{
			bindingContext.ModelState.TryAddModelError(
				bindingContext.ModelName,
				"Invalid date format");
			return Task.CompletedTask;
		}

		bindingContext.Result = ModelBindingResult.Success(dateTime.ToUtc());
		return Task.CompletedTask;
	}
}
""";
		}

		public static string DateTimeModelBinderProvider(string projectName)
		{
			return $$"""
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace {{projectName}}.SharedKernel.Middleware;

public sealed class DateTimeModelBinderProvider : IModelBinderProvider
{
	public IModelBinder? GetBinder(ModelBinderProviderContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		var type = context.Metadata.ModelType;
		var isDate = type == typeof(DateTime) || type == typeof(DateTime?);
		return isDate ? new DateTimeModelBinder() : null;
	}
}
""";
		}

		public static string RequestGuidMiddleware(string projectName)
		{
			return $$"""
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using {{projectName}}.SharedKernel.Common;

namespace {{projectName}}.SharedKernel.Middleware;

public sealed class RequestGuidMiddleware
{
	private readonly RequestDelegate _next;

	public RequestGuidMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task Invoke(HttpContext context)
	{
		var requestId = Guid.NewGuid();
		context.Items[Constants.HttpContextKeys.REQUEST_ID_KEY] = requestId;

		using (LogContext.PushProperty(
			Constants.HttpContextKeys.REQUEST_ID_KEY,
			requestId))
		{
			context.Response.Headers.Append(
				"X-RequestId",
				requestId.ToString());
			await _next(context);
		}
	}
}
""";
		}

		public static string GlobalExceptionHandlingMiddleware(
			string projectName)
		{
			return $$"""
using System.Net;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using {{projectName}}.SharedKernel.Common;
using {{projectName}}.SharedKernel.Exceptions;

namespace {{projectName}}.SharedKernel.Middleware;

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
""";
		}

		public static string ModelStateValidationFilter(string projectName)
		{
			return $$"""
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using {{projectName}}.SharedKernel.Common;
using {{projectName}}.SharedKernel.Exceptions;

namespace {{projectName}}.SharedKernel.Middleware;

public sealed class ModelStateValidationFilter : IActionFilter
{
	public void OnActionExecuting(ActionExecutingContext context)
	{
		if (context.ModelState.IsValid)
		{
			return;
		}

		var requestId = context.HttpContext.Items[
			Constants.HttpContextKeys.REQUEST_ID_KEY] as Guid?;

		var errors = context.ModelState
			.Where(ms => ms.Value?.Errors.Count > 0)
			.ToDictionary(
				kvp => kvp.Key,
				kvp => kvp.Value?.Errors
					.Select(e => e.ErrorMessage)
					.ToArray());

		Log.Warning(
			"ModelState validation failed for request {RequestId}.",
			requestId);

		var messages = string.Join(
			"; ",
			errors.SelectMany(e => e.Value ?? Array.Empty<string>()));

		context.Result = new BadRequestObjectResult(
			new GenericExceptionModel
			{
				RequestGuid = requestId ?? Guid.Empty,
				ErrorMessage = $"Validation failed: {messages}"
			});
	}

	public void OnActionExecuted(ActionExecutedContext context)
	{
	}
}
""";
		}

		public static string DependencyInjection(string projectName)
		{
			return $$"""
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {{projectName}}.SharedKernel;

public static class DependencyInjection
{
	public static IServiceCollection AddSharedKernelDI(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		return services;
	}
}
""";
		}
	}
}
