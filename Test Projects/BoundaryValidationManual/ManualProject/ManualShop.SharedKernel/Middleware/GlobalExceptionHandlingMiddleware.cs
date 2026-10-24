using System.Net;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using ManualShop.SharedKernel.Common;
using ManualShop.SharedKernel.Exceptions;

namespace ManualShop.SharedKernel.Middleware;

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