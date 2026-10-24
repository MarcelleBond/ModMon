namespace ManualShop.SharedKernel.Events;

public interface IEvent
{
	Guid EventId { get; }
	DateTime OccurredAt { get; }
}