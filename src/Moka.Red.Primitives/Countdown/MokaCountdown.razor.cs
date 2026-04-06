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
	private int _days, _hours, _minutes, _seconds;
	private bool _disposed;
	private bool _isComplete;
	private Timer? _timer;

	/// <summary>The target date/time to count down to. Required.</summary>
	[Parameter]
	[EditorRequired]
	public DateTime TargetDate { get; set; }

	/// <summary>Fires when the countdown reaches zero.</summary>
	[Parameter]
	public EventCallback OnComplete { get; set; }

	/// <summary>Whether to show the days unit. Default true.</summary>
	[Parameter]
	public bool ShowDays { get; set; } = true;

	/// <summary>Whether to show the hours unit. Default true.</summary>
	[Parameter]
	public bool ShowHours { get; set; } = true;

	/// <summary>Whether to show the minutes unit. Default true.</summary>
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

	/// <summary>Override ShouldRender to always return true for timer-driven updates.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		UpdateCountdown();
		_timer = new Timer(OnTick, null, 1000, 1000);
	}

	private void OnTick(object? state)
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			InvokeAsync(() =>
			{
				if (_disposed)
				{
					return;
				}

				UpdateCountdown();
				StateHasChanged();
			});
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private void UpdateCountdown()
	{
		TimeSpan remaining = TargetDate - DateTime.Now;
		if (remaining <= TimeSpan.Zero)
		{
			_days = _hours = _minutes = _seconds = 0;
			if (!_isComplete)
			{
				_isComplete = true;
				if (OnComplete.HasDelegate)
				{
					_ = OnComplete.InvokeAsync();
				}
			}

			_timer?.Dispose();
			return;
		}

		_days = remaining.Days;
		_hours = remaining.Hours;
		_minutes = remaining.Minutes;
		_seconds = remaining.Seconds;
	}

	private string GetLabel(string full, string compact) => CompactLabels ? compact : full;

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
}
