using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using ManualShop.SharedKernel.Common;
using ManualShop.SharedKernel.Exceptions;

namespace ManualShop.SharedKernel.Middleware;

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