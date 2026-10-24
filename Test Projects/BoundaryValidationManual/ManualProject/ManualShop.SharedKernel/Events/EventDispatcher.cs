using Microsoft.Extensions.DependencyInjection;

namespace ManualShop.SharedKernel.Events;

public sealed class EventDispatcher : IEventDispatcher
{
	private readonly IServiceProvider _serviceProvider;

	public EventDispatcher(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public async Task DispatchAsync<TEvent>(
		TEvent @event,
		CancellationToken cancellationToken = default)
		where TEvent : IEvent
	{
		var handlers = _serviceProvider
			.GetServices<IEventHandler<TEvent>>();

		foreach (var handler in handlers)
		{
			await handler.HandleAsync(@event, cancellationToken);
		}
	}
}