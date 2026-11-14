using Microsoft.AspNetCore.Components;

namespace Moka.Red.Core.Base;

/// <summary>
///     The value an input shows, kept apart from the value parameter its parent passes, so the input never writes its own
///     parameter. A parent that passes the value and listens to the change callback owns the value: a change the person
///     makes shows while the parent's handler runs, and once the handler has finished the input shows the parent's value
///     again unless the parent passed a new one. A parent that passes no value, or doesn't listen, leaves the input its own
///     state, which follows the parent only when it passes a value different from the last one.
/// </summary>
/// <remarks>
///     The owning component calls <see cref="Take(ParameterView, string)" /> from <c>SetParametersAsync</c> before the
///     parameters are applied, renders <see cref="Shown" />, calls <see cref="Pick" /> for a change the person makes,
///     <see cref="AfterRender" /> from <c>OnAfterRender</c> and <see cref="Stop" /> when it is disposed. A native element
///     that holds the value in the browser binds it with <c>@bind:get</c> and <c>@bind:set</c>, so the render that puts a
///     refused value back reaches the screen.
/// </remarks>
/// <typeparam name="TValue">The type of the value.</typeparam>
public sealed class MokaInputValue<TValue>
{
	private readonly IEqualityComparer<TValue?> _comparer;
	private readonly Func<Exception, Task> _fail;
	private readonly Action _render;
	private int _answered;
	private int _change;
	private TValue? _given;
	private bool _stopped;
	private bool _wasGiven;

	/// <summary>Creates the holder for one input.</summary>
	/// <param name="render">Asks the input to render, for when it goes back to the parent's value.</param>
	/// <param name="fail">
	///     Reports a failure in the parent's handler that arrives after the change, such as the input's
	///     <c>DispatchExceptionAsync</c>, so it reaches the nearest error boundary.
	/// </param>
	/// <param name="initial">What the input shows before the parent passes a value.</param>
	/// <param name="comparer">Decides whether two values are the same. Defaults to the type's own equality.</param>
	public MokaInputValue(Action render, Func<Exception, Task> fail, TValue? initial = default,
		IEqualityComparer<TValue?>? comparer = null)
	{
		ArgumentNullException.ThrowIfNull(render);
		ArgumentNullException.ThrowIfNull(fail);
		_render = render;
		_fail = fail;
		_comparer = comparer ?? EqualityComparer<TValue?>.Default;
		Shown = initial;
	}

	/// <summary>The value the input shows.</summary>
	public TValue? Shown { get; private set; }

	/// <summary>
	///     Takes the value parameter from a parameter set, before the parameters are applied. The input shows a passed
	///     value when it differs from the last one the parent passed, so a parent render that passes the same value again
	///     leaves a change in progress showing.
	/// </summary>
	/// <param name="parameters">The parameters the parent passed.</param>
	/// <param name="name">The name of the value parameter.</param>
	/// <returns>Whether the shown value changed.</returns>
	public bool Take(ParameterView parameters, string name) =>
		parameters.TryGetValue(name, out TValue? given) && Take(given);

	/// <summary>
	///     Takes a value the parent passed, for an input that works it out from several parameters, such as a range from
	///     its start and its end. Call it before the render, from <c>OnParametersSet</c>, only when the parent passed one.
	/// </summary>
	/// <param name="given">The value the parent passed.</param>
	/// <returns>Whether the shown value changed.</returns>
	public bool Take(TValue? given)
	{
		bool isNew = !_wasGiven || !_comparer.Equals(given, _given);
		_given = given;
		_wasGiven = true;
		if (!isNew || _comparer.Equals(Shown, given))
		{
			return false;
		}

		Shown = given;
		return true;
	}

	/// <summary>
	///     Shows a value the person picked and raises <paramref name="changed" /> with it. When the parent owns the value
	///     and has not passed a new one by the time its handler finishes, the input shows the parent's value again: right
	///     after the next render when the handler finished at once, so that render can still read the picked value, or as
	///     the handler finishes when it ran on. Only the latest change counts.
	/// </summary>
	/// <param name="value">The value the person picked.</param>
	/// <param name="changed">The input's change callback.</param>
	public void Pick(TValue? value, EventCallback<TValue> changed)
	{
		Shown = value;
		if (!changed.HasDelegate)
		{
			return;
		}

		bool owned = _wasGiven;
		int change = ++_change;
		Task answer = changed.InvokeAsync(value!);
		if (answer.IsCompletedSuccessfully)
		{
			if (owned)
			{
				_answered = change;
			}

			return;
		}

		_ = AwaitAnswerAsync(answer, change, owned);
	}

	/// <summary>
	///     Shows the parent's value again when its handler for the latest change finished at once and it passed no new
	///     value. Call it from the input's <c>OnAfterRender</c>.
	/// </summary>
	public void AfterRender()
	{
		if (_answered == 0 || _stopped)
		{
			return;
		}

		bool latest = _answered == _change;
		_answered = 0;
		if (latest)
		{
			ShowGiven();
		}
	}

	private async Task AwaitAnswerAsync(Task answer, int change, bool owned)
	{
		try
		{
			await answer;
		}
		catch (Exception ex) when (!answer.IsCanceled)
		{
			if (!_stopped)
			{
				await _fail(ex);
			}
		}
		catch (OperationCanceledException)
		{
			// A cancelled handler is no failure, as for any event handler, and the input still follows the parent.
		}

		if (owned && change == _change && !_stopped)
		{
			ShowGiven();
		}
	}

	/// <summary>
	///     Stops following the parent, for when the input is disposed: a handler that finishes afterwards no longer
	///     renders the input, and a failure in it has no input left to report to. Call it from the input's disposal.
	/// </summary>
	public void Stop() => _stopped = true;

	private void ShowGiven()
	{
		if (_comparer.Equals(Shown, _given))
		{
			return;
		}

		Shown = _given;
		_render();
	}
}
