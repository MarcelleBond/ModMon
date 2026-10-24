namespace ManualShop.SharedKernel.Exceptions;

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