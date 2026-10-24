namespace ManualShop.SharedKernel.Events;

public interface IEventDispatcher
{
	Task DispatchAsync<TEvent>(
		TEvent @event,
		CancellationToken cancellationToken = default)
		where TEvent : IEvent;
}