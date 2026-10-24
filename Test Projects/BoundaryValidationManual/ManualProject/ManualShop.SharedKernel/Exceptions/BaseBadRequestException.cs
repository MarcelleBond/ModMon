namespace ManualShop.SharedKernel.Exceptions;

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