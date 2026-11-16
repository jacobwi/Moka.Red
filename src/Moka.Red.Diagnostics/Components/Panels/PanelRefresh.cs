namespace Moka.Red.Diagnostics.Components.Panels;

// Reloads a panel's data on a timer and renders the panel only when what it shows changed. The timer runs on the
// thread pool, so the reload goes through the panel's InvokeAsync, and a failure reaches Blazor's error handling through
// the panel's DispatchExceptionAsync rather than vanishing with a discarded task.
internal sealed class PanelRefresh : IDisposable
{
	private readonly Func<Action, Task> _dispatch;
	private readonly Func<Exception, Task> _fail;
	private readonly Func<bool> _reload;
	private readonly Action _render;
	private readonly ITimer _timer;
	private volatile bool _stopped;

	public PanelRefresh(TimeProvider clock, TimeSpan every, Func<Action, Task> dispatch, Func<Exception, Task> fail,
		Func<bool> reload, Action render)
	{
		_dispatch = dispatch;
		_fail = fail;
		_reload = reload;
		_render = render;
		_timer = clock.CreateTimer(_ => _ = TickAsync(), null, every, every);
	}

	public void Dispose()
	{
		_stopped = true;
		_timer.Dispose();
	}

	private async Task TickAsync()
	{
		if (_stopped)
		{
			return;
		}

		try
		{
			await _dispatch(() =>
			{
				if (!_stopped && _reload())
				{
					_render();
				}
			});
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away between the tick and the dispatch.
		}
		catch (Exception ex) when (!_stopped)
		{
			await _fail(ex);
		}
	}
}
