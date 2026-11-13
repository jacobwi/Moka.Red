using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Primitives.RelativeTime;

/// <summary>
///     A moment as words, such as "3 minutes ago" or "in 2 hours", that keeps itself current. Each one wakes only when
///     its own text is due to change, so a table can hold thousands. The words are English; <see cref="Formatter" />
///     replaces them. It takes the size and color of the text around it.
/// </summary>
public partial class MokaRelativeTime : MokaVisualComponentBase
{
	// HTML's datetime allows at most three digits of a second, so the round-trip "O" format doesn't qualify.
	private const string MachineFormat = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";

	private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);
	private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
	private static readonly TimeSpan Day = TimeSpan.FromDays(1);
	private static readonly TimeSpan Week = TimeSpan.FromDays(7);
	private static readonly TimeSpan Month = TimeSpan.FromDays(30);
	private static readonly TimeSpan Year = TimeSpan.FromDays(365);

	private static readonly (TimeSpan Size, TimeSpan Until, string Name)[] Units =
	[
		(Minute, Hour, "minute"),
		(Hour, Day, "hour"),
		(Day, Week, "day"),
		(Week, Month, "week"),
		(Month, Year, "month"),
		(Year, TimeSpan.MaxValue, "year")
	];

	// A timer throws on a wait of 49.7 days or more. A value due later wakes at this, finds nothing to do and waits again.
	private static readonly TimeSpan LongestWait = TimeSpan.FromDays(30);

	// Whole units are counted down, so a future value sitting exactly on one still reads the same until just after it.
	private static readonly TimeSpan JustPast = TimeSpan.FromMilliseconds(1);

	private TimeProvider? _clock;
	private string _text = string.Empty;
	private ITimer? _timer;
	private DateTimeOffset? _wakeFor;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	/// <summary>The moment to describe. Null shows <see cref="EmptyText" />.</summary>
	[Parameter]
	public DateTimeOffset? Value { get; set; }

	/// <summary>Keeps the text current as time passes. Default true. Off, it's only worked out when the parent renders.</summary>
	[Parameter]
	public bool Live { get; set; } = true;

	/// <summary>
	///     Once the value is this far away, in either direction, a date in <see cref="Format" /> takes the place of the
	///     words. Null, the default, keeps the words however far away it is.
	/// </summary>
	[Parameter]
	public TimeSpan? AbsoluteAfter { get; set; }

	/// <summary>The format of the date shown past <see cref="AbsoluteAfter" />. Default "d", the culture's short date.</summary>
	[Parameter]
	public string Format { get; set; } = "d";

	/// <summary>
	///     Writes the words in place of the English ones, for another language or other wording. It's given how long ago
	///     the value was, negative for one still to come, and asked again whenever the English words would have changed.
	/// </summary>
	[Parameter]
	public Func<TimeSpan, string>? Formatter { get; set; }

	/// <summary>Shown when <see cref="Value" /> is null. Default "-".</summary>
	[Parameter]
	public string EmptyText { get; set; } = "-";

	/// <summary>Shows the full date and time in a tooltip on hover. Default true.</summary>
	[Parameter]
	public bool ShowTooltip { get; set; } = true;

	/// <summary>The format of the tooltip's date and time. Default "G", the culture's date and time with seconds.</summary>
	[Parameter]
	public string TooltipFormat { get; set; } = "G";

	/// <summary>Where the tooltip opens. Default Top.</summary>
	[Parameter]
	public MokaTooltipPosition TooltipPosition { get; set; } = MokaTooltipPosition.Top;

	/// <summary>
	///     The zone the tooltip's date and time, and the date past <see cref="AbsoluteAfter" />, are written in. Null uses
	///     a registered <see cref="TimeProvider" />'s <see cref="TimeProvider.LocalTimeZone" />, else
	///     <see cref="TimeZoneInfo.Local" />, which on Blazor Server is the server's, as a chart's time axis does. The
	///     words and the machine-readable <c>datetime</c> don't depend on it.
	/// </summary>
	[Parameter]
	public TimeZoneInfo? TimeZone { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-relative-time";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("font-size", FontSize)
		.AddStyle("color", TextColor)
		.AddStyle(Style)
		.Build();

	// An app can register its own clock. A test does, so no test waits on a real minute.
	private TimeProvider Clock => _clock ??= Services.GetService<TimeProvider>() ?? TimeProvider.System;

	// The system clock's zone is TimeZoneInfo.Local, so this is the chain a chart's time axis follows.
	private TimeZoneInfo Zone => TimeZone ?? Clock.LocalTimeZone;

	// Md, the base class's default, is what no choice looks like, so it leaves the size to the text around it.
	private string? FontSize => SizeValue is not null
		? CssValues.WithPixels(SizeValue)
		: Size == MokaSize.Md ? null : MokaEnumHelpers.ToFontSize(Size);

	// Surface means the text color on a surface. The surface color itself would be invisible on one.
	private string? TextColor => Color switch
	{
		null => null,
		MokaColor.Surface => "var(--moka-color-on-surface)",
		MokaColor color => $"var(--moka-color-{ColorToKebab(color)})"
	};

	private string? MachineTime => Value?.ToString(MachineFormat, CultureInfo.InvariantCulture);

	private string? Absolute
	{
		get
		{
			if (!ShowTooltip || Value is not { } value)
			{
				return null;
			}

			string absolute = InZone(value).ToString(TooltipFormat, CultureInfo.CurrentCulture);
			return absolute == _text ? null : absolute;
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		UpdateText();
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		Arm();
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_timer is not null)
		{
			await _timer.DisposeAsync();
			_timer = null;
		}

		await base.DisposeAsyncCore();
	}

	private static string Words(TimeSpan age)
	{
		TimeSpan distance = age.Duration();
		if (distance < Minute)
		{
			return "just now";
		}

		(TimeSpan size, _, string name) = UnitFor(distance);
		long count = distance.Ticks / size.Ticks;
		string amount = string.Create(CultureInfo.InvariantCulture, $"{count} {name}{(count == 1 ? "" : "s")}");
		return age < TimeSpan.Zero ? $"in {amount}" : $"{amount} ago";
	}

	private static (TimeSpan Size, TimeSpan Until, string Name) UnitFor(TimeSpan distance) =>
		Units.First(unit => distance < unit.Until);

	// The age, always moving up, at which the words next read differently. A value to come counts down to a whole unit.
	private static TimeSpan NextWordsChange(TimeSpan age)
	{
		TimeSpan distance = age.Duration();
		if (distance < Minute)
		{
			return Minute;
		}

		(TimeSpan size, TimeSpan until, _) = UnitFor(distance);
		long count = distance.Ticks / size.Ticks;
		if (age < TimeSpan.Zero)
		{
			return TimeSpan.FromTicks(-size.Ticks * count);
		}

		TimeSpan nextCount = TimeSpan.FromTicks(size.Ticks * (count + 1));
		return nextCount < until ? nextCount : until;
	}

	private bool ShowsDate(TimeSpan age) => AbsoluteAfter is { } limit && age.Duration() >= limit;

	private DateTimeOffset InZone(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Zone);

	private TimeSpan? NextChange(TimeSpan age)
	{
		if (AbsoluteAfter is not { } limit)
		{
			return NextWordsChange(age);
		}

		if (limit <= TimeSpan.Zero)
		{
			return null;
		}

		if (ShowsDate(age))
		{
			return age < TimeSpan.Zero ? -limit : null;
		}

		TimeSpan next = NextWordsChange(age);
		return next < limit ? next : limit;
	}

	private bool UpdateText()
	{
		string text = Compose();
		if (text == _text)
		{
			return false;
		}

		_text = text;
		return true;
	}

	private string Compose()
	{
		if (Value is not { } value)
		{
			return EmptyText;
		}

		TimeSpan age = Clock.GetUtcNow() - value;
		if (ShowsDate(age))
		{
			return InZone(value).ToString(Format, CultureInfo.CurrentCulture);
		}

		return Formatter is { } write ? write(age) : Words(age);
	}

	// Runs after every render, and a parent's renders change nothing here: the same value is due at the same moment.
	private void Arm()
	{
		if (Value is not { } value || !Live || !RendererInfo.IsInteractive)
		{
			Disarm();
			return;
		}

		DateTimeOffset now = Clock.GetUtcNow();
		if (NextChange(now - value) is not { } next)
		{
			Disarm();
			return;
		}

		DateTimeOffset due = value + next;
		if (due == _wakeFor)
		{
			return;
		}

		_wakeFor = due;
		TimeSpan untilDue = due - now + JustPast;
		TimeSpan wait = untilDue < LongestWait ? untilDue : LongestWait;
		if (_timer is null)
		{
			_timer = Clock.CreateTimer(_ => RunDetached(Wake), null, wait, Timeout.InfiniteTimeSpan);
		}
		else
		{
			_timer.Change(wait, Timeout.InfiniteTimeSpan);
		}
	}

	private void Disarm()
	{
		_timer?.Dispose();
		_timer = null;
		_wakeFor = null;
	}

	private void Wake()
	{
		_wakeFor = null;
		if (UpdateText())
		{
			StateHasChanged();
		}

		Arm();
	}
}
