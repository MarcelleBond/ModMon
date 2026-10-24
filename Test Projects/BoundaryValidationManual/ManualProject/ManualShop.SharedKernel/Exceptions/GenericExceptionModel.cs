namespace ManualShop.SharedKernel.Exceptions;

public sealed class GenericExceptionModel
{
	public Guid RequestGuid { get; set; }
	public required string ErrorMessage { get; set; }
}