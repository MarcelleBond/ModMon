using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ManualShop.SharedKernel.Middleware;

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