namespace ManualShop.SharedKernel.Exceptions;

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