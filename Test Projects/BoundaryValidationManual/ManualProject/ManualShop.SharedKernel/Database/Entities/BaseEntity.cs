namespace ManualShop.SharedKernel.Database.Entities;

public abstract class BaseEntity
{
	private DateTime? _createdDate;
	private DateTime? _modifiedDate;

	public Guid Id { get; set; }
	public string? CreationUserId { get; set; }
	public string? ModificationUserId { get; set; }

	public DateTime? CreatedDate
	{
		get => _createdDate;
		set => _createdDate = value?.ToUniversalTime();
	}

	public DateTime? ModifiedDate
	{
		get => _modifiedDate;
		set => _modifiedDate = value?.ToUniversalTime();
	}

	protected void EnsureDateTimeKind(ref DateTime? dateTime)
	{
		if (dateTime.HasValue && dateTime.Value.Kind == DateTimeKind.Unspecified)
		{
			dateTime = DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
		}
	}

	protected void EnsureDateTimeKind(ref DateTime dateTime)
	{
		if (dateTime.Kind == DateTimeKind.Unspecified)
		{
			dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
		}
	}
}