using Microsoft.AspNetCore.Components;

namespace Moka.Red.ThemeGen.Studio;

// A handler whose target isn't a component, so handling it renders nothing: blazor/performance/rendering.md, "Avoid
// rerendering after handling events without state changes" (EventUtil.AsNonRenderingEventHandler). The callback factory
// takes the handler's target as the receiver when it handles events itself. The studio queues a slider's values this
// way and draws once a frame. Nothing it wraps throws, which matters since the approach doesn't reach an error boundary.
internal static class NonRendering
{
	public static Action<T> Handler<T>(Action<T> callback) => new Receiver<T>(callback).Invoke;

	private sealed record Receiver<T>(Action<T> Callback) : IHandleEvent
	{
		public void Invoke(T arg) => Callback(arg);

		public Task HandleEventAsync(EventCallbackWorkItem item, object? arg) => item.InvokeAsync(arg);
	}
}
