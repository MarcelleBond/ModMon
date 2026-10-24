using Microsoft.AspNetCore.Http;
using Serilog.Context;
using ManualShop.SharedKernel.Common;

namespace ManualShop.SharedKernel.Middleware;

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