namespace ManualShop.SharedKernel.Extensions;

public static class DateTimeExtensions
{
	public static DateTime ToUtc(this DateTime value)
	{
		return value.Kind == DateTimeKind.Utc
			? value
			: DateTime.SpecifyKind(value, DateTimeKind.Utc);
	}
}