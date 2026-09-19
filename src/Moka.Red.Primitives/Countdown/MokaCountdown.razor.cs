using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Countdown;

/// <summary>
///     Animated countdown timer displaying days, hours, minutes, and seconds
///     with configurable styles: Boxes, Inline, or Flip.
/// </summary>
public partial class MokaCountdown : MokaVisualComponentBase
{
	// How long after a whole second of the remaining time the timer wakes. Enough to be sure the
	// second has passed, too little to see.
	private static readonly TimeSpan TickSlack = TimeSpan.FromMilliseconds(20);

	private readonly CountdownUnit[] _units =
	[
		new("Days", "d", TimeSpan.TicksPerDay),
		new("Hours", "h", TimeSpan.TicksPerHour),
		new("Minutes", "m", TimeSpan.TicksPerMinute),
		new("Seconds", "s", TimeSpan.TicksPerSecond)
	];

	private bool _completionPending;
	private bool _disposed;
	private bool _hasTarget;
	private bool _isComplete;
	private DateTime _targetUtc;
	private Timer? _timer;

	/// <summary>
	///     The target date/time to count down to. Required. A <see cref="DateTimeKind.Utc" /> value is read as
	///     UTC, a <see cref="DateTimeKind.Local" /> or <see cref="DateTimeKind.Unspecified" /> one as local time.
	///     A new value starts the countdown over, also after it has completed.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public DateTime TargetDate { get; set; }

	/// <summary>
	///     Fires once when the countdown reaches zero, after the render that shows <see cref="CompletedText" />.
	///     A target that has already passed fires it after the first render. Exceptions from the handler go
	///     to Blazor's error handling.
	/// </summary>
	[Parameter]
	public EventCallback OnComplete { get; set; }

	/// <summary>
	///     Whether to show the days unit. Default true. When hidden, whole days are counted in the first
	///     shown unit instead, so 2 days and 3 hours reads as 51 hours.
	/// </summary>
	[Parameter]
	public bool ShowDays { get; set; } = true;

	/// <summary>Whether to show the hours unit. Default true. When hidden, its time moves into the next shown unit.</summary>
	[Parameter]
	public bool ShowHours { get; set; } = true;

	/// <summary>Whether to show the minutes unit. Default true. When hidden, its time moves into the seconds.</summary>
	[Parameter]
	public bool ShowMinutes { get; set; } = true;

	/// <summary>Whether to show the seconds unit. Default true.</summary>
	[Parameter]
	public bool ShowSeconds { get; set; } = true;

	/// <summary>Whether to show labels below each unit. Default true.</summary>
	[Parameter]
	public bool ShowLabels { get; set; } = true;

	/// <summary>Use compact labels ("d", "h", "m", "s") instead of full words. Default false.</summary>
	[Parameter]
	public bool CompactLabels { get; set; }

	/// <summary>Separator string between units. Default ":".</summary>
	[Parameter]
	public string Separator { get; set; } = ":";

	/// <summary>Text shown when the countdown completes. Default "Time's up!".</summary>
	[Parameter]
	public string CompletedText { get; set; } = "Time's up!";

	/// <summary>Visual style of the countdown. Default Boxes.</summary>
	[Parameter]
	public MokaCountdownStyle CountdownStyle { get; set; } = MokaCountdownStyle.Boxes;

	/// <inheritdoc />
	protected override string RootClass => "moka-countdown";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-countdown--{MokaEnumHelpers.ToCssClass(CountdownStyle)}")
		.AddClass("moka-countdown--complete", _isComplete)
		.AddClass(Class)
		.Build();

	// The root only lines the units up and draws nothing. In the Boxes style each unit draws a box
	// and in the Flip style each value draws a card, so the radius goes on those. The Inline style
	// draws no boxes and keeps it on the root, for a background given through Style or Class.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding, CountdownStyle == MokaCountdownStyle.Inline)
		.AddStyle(Style)
		.Build();

	private string? UnitStyle => BoxStyle(CountdownStyle == MokaCountdownStyle.Boxes);

	private string? ValueStyle => BoxStyle(Flips);

	private bool Flips => CountdownStyle == MokaCountdownStyle.Flip;

	/// <summary>Override ShouldRender to always return true for timer-driven updates.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// DateTime equality ignores Kind, so compare the instants.
		DateTime targetUtc = ToUtc(TargetDate);
		if (!_hasTarget || targetUtc != _targetUtc)
		{
			_hasTarget = true;
			_targetUtc = targetUtc;
			_isComplete = false;
			_completionPending = false;
		}

		// Which units are shown decides where hidden time goes, so every parameter set counts again.
		Refresh();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!_completionPending)
		{
			return;
		}

		_completionPending = false;

		// The renderer awaits this, so the handler's exceptions reach Blazor's error handling rather
		// than vanishing with a discarded task.
		await OnComplete.InvokeAsync();
	}

	private static DateTime ToUtc(DateTime value) =>
		value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

	private bool IsShown(int unitIndex) => unitIndex switch
	{
		0 => ShowDays,
		1 => ShowHours,
		2 => ShowMinutes,
		_ => ShowSeconds
	};

	private void Refresh()
	{
		TimeSpan remaining = _targetUtc - DateTime.UtcNow;
		if (remaining <= TimeSpan.Zero)
		{
			StopTimer();
			SetUnits(TimeSpan.Zero);
			if (!_isComplete)
			{
				_isComplete = true;
				_completionPending = true;
			}

			return;
		}

		SetUnits(remaining);
		ScheduleTick(remaining);
	}

	// Walks the units from days down. Time in a hidden unit stays in the pool for the next shown one,
	// so hiding days turns 2 days and 3 hours into 51 hours.
	private void SetUnits(TimeSpan remaining)
	{
		long ticks = Math.Max(0, remaining.Ticks);
		for (int i = 0; i < _units.Length; i++)
		{
			if (!IsShown(i))
			{
				continue;
			}

			CountdownUnit unit = _units[i];
			long value = ticks / unit.Ticks;
			ticks -= value * unit.Ticks;
			unit.Set(value, HasRendered);
		}
	}

	private void ScheduleTick(TimeSpan remaining)
	{
		if (_disposed)
		{
			return;
		}

		// Wake just after the next whole second of the remaining time. A fixed one-second period drifts
		// against the clock, and a tick that lands just before a second shows one value twice and then
		// skips the next.
		TimeSpan due = TimeSpan.FromTicks(remaining.Ticks % TimeSpan.TicksPerSecond) + TickSlack;
		if (_timer is null)
		{
			_timer = new Timer(OnTick, null, due, Timeout.InfiniteTimeSpan);
		}
		else
		{
			_timer.Change(due, Timeout.InfiniteTimeSpan);
		}
	}

	private void StopTimer()
	{
		_timer?.Dispose();
		_timer = null;
	}

	private void OnTick(object? state) => _ = TickAsync();

	private async Task TickAsync()
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			// The timer fires on a thread-pool thread; state only changes on the renderer's.
			await InvokeAsync(() =>
			{
				if (_disposed)
				{
					return;
				}

				Refresh();
				StateHasChanged();
			});
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away between the tick and the dispatch.
		}
		catch (Exception ex) when (!_disposed)
		{
			await DispatchExceptionAsync(ex);
		}
	}

	private string LabelFor(CountdownUnit unit) => CompactLabels ? unit.CompactLabel : unit.Label;

	private static string FormatValue(CountdownUnit unit) => unit.Value.ToString("D2", CultureInfo.CurrentCulture);

	// In the flip style each value is its own element, so a changed digit mounts fresh and its CSS
	// animation runs. A null key means no key, and the other styles keep one element per unit.
	private long? ValueKey(CountdownUnit unit) => Flips ? unit.Value : null;

	private string ValueClass(CountdownUnit unit) => new CssBuilder("moka-countdown-value")
		.AddClass("moka-countdown-value--changed", Flips && unit.HasChanged)
		.Build();

	private string? BoxStyle(bool drawsTheBox) => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding, drawsTheBox)
		.Build();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		if (_timer is not null)
		{
			await _timer.DisposeAsync();
			_timer = null;
		}

		await base.DisposeAsyncCore();
	}

	private sealed class CountdownUnit(string label, string compactLabel, long ticks)
	{
		public string Label { get; } = label;

		public string CompactLabel { get; } = compactLabel;

		public long Ticks { get; } = ticks;

		public long Value { get; private set; }

		// True once the value has changed on screen. The first render shows its values without a flip.
		public bool HasChanged { get; private set; }

		public void Set(long value, bool rendered)
		{
			if (rendered && value != Value)
			{
				HasChanged = true;
			}

			Value = value;
		}
	}
}
