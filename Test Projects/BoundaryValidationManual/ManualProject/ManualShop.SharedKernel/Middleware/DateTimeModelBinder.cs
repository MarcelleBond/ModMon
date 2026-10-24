using Microsoft.AspNetCore.Mvc.ModelBinding;
using ManualShop.SharedKernel.Extensions;

namespace ManualShop.SharedKernel.Middleware;

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