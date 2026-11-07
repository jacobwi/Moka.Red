using Microsoft.AspNetCore.Components;

namespace Moka.Red.Data.Table;

/// <summary>
///     Event callbacks that do not redraw the component they belong to. A handler normally goes through
///     the component's own <see cref="IHandleEvent" />, which renders afterwards, so a handler that changes
///     nothing on screen costs a whole table's diff. These go through a receiver that only runs the handler.
/// </summary>
/// <remarks>
///     An exception thrown here does not reach an error boundary, so keep consumer code out of these
///     handlers. A shared receiver keeps the callback equal from render to render, so its handler id survives.
/// </remarks>
internal static class NonRendering
{
	private static readonly Receiver Shared = new();

	public static EventCallback<T> Callback<T>(Action<T> handler) => new(Shared, handler);

	private sealed class Receiver : IHandleEvent
	{
		public Task HandleEventAsync(EventCallbackWorkItem item, object? arg) => item.InvokeAsync(arg);
	}
}
