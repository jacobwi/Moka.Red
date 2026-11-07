using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.ActionBar;

/// <summary>
///     A floating bar for something waiting on the person: what it is, what they can do, how long they have, and room
///     for a list of what it changes. For a count of picked items use <c>MokaSelectionBar</c>.
/// </summary>
public partial class MokaActionBar : MokaVisualComponentBase
{
	private const string SpaceModule = "./_content/Moka.Red.Core/moka-space.js";

	private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
	private static readonly TimeSpan UrgentBelow = TimeSpan.FromSeconds(15);

	private ElementReference _bar;
	private bool _binding;
	private DateTimeOffset _endsAt;
	private TimeSpan? _lastCountdown;
	private TimeSpan _left;
	private TimeSpan _longest;
	private IJSObjectReference? _space;
	private string? _spaceFor;
	private ITimer? _tick;
	private bool _wasVisible;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	/// <summary>Shows the bar.</summary>
	[Parameter]
	public bool Visible { get; set; }

	/// <summary>At the start of the bar. <see cref="Countdown" /> takes its place.</summary>
	[Parameter]
	public RenderFragment? Icon { get; set; }

	/// <summary>What's waiting, in a strong line.</summary>
	[Parameter]
	public RenderFragment? Title { get; set; }

	/// <summary>More about it, under the title.</summary>
	[Parameter]
	public RenderFragment? Detail { get; set; }

	/// <summary>The buttons, at the end of the bar.</summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>Above the row, for a list of what's waiting. It scrolls past 340 px.</summary>
	[Parameter]
	public RenderFragment? Body { get; set; }

	/// <summary>
	///     Time left, drawn as a ring in place of <see cref="Icon" />. The bar counts it down itself from each new value it
	///     is given, and from the start again each time it is shown. Passing a new value every second works too.
	/// </summary>
	[Parameter]
	public TimeSpan? Countdown { get; set; }

	/// <summary>What a full ring stands for. Left out, it's the longest <see cref="Countdown" /> since the bar was shown.</summary>
	[Parameter]
	public TimeSpan? Duration { get; set; }

	/// <summary>Raised once when the bar's own count reaches zero.</summary>
	[Parameter]
	public EventCallback OnCountdownEnd { get; set; }

	/// <summary>The bar's name for screen readers.</summary>
	[Parameter]
	public string Label { get; set; } = "Waiting on you";

	/// <summary>Said before the time for screen readers, as "Time left: 0:42".</summary>
	[Parameter]
	public string CountdownLabel { get; set; } = "Time left";

	/// <inheritdoc />
	protected override string RootClass => "moka-action-bar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	// An app can register its own clock. A test does, so no test waits on a real second.
	private TimeProvider Clock => Services.GetService<TimeProvider>() ?? TimeProvider.System;

	private bool Counting => Visible && Countdown is not null && _left > TimeSpan.Zero;

	private string RingClass => new CssBuilder("moka-action-bar__ring")
		.AddClass("moka-action-bar__ring--urgent", _left < UrgentBelow)
		.Build();

	private string? RingStyle => new StyleBuilder()
		.AddStyle("--_ring-fill", $"{(RingFraction * 100).ToString("0.##", CultureInfo.InvariantCulture)}%")
		.Build();

	private double RingFraction
	{
		get
		{
			TimeSpan full = Duration ?? _longest;
			return full <= TimeSpan.Zero ? 0 : Math.Clamp(_left / full, 0, 1);
		}
	}

	private string RingText
	{
		get
		{
			int seconds = (int)Math.Ceiling(Math.Max(0, _left.TotalSeconds));
			return string.Create(CultureInfo.InvariantCulture, $"{seconds / 60}:{seconds % 60:00}");
		}
	}

	private string RingLabel => $"{CountdownLabel}: {RingText}";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		bool shown = Visible && !_wasVisible;
		_wasVisible = Visible;
		if (Countdown == _lastCountdown && !shown)
		{
			return;
		}

		_lastCountdown = Countdown;
		if (Countdown is { } countdown)
		{
			_left = countdown;
			_endsAt = Clock.GetUtcNow() + countdown;
			_longest = shown || countdown > _longest ? countdown : _longest;
		}
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);

		// Here rather than with the parameters: this doesn't run while prerendering, and that render would leave a timer behind.
		if (!Counting)
		{
			StopTicking();
		}
		else if (_tick is null)
		{
			_tick = Clock.CreateTimer(_ => RunDetached(TickAsync), null, OneSecond, OneSecond);
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		await SyncSpaceAsync();
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_tick is not null)
		{
			await _tick.DisposeAsync();
			_tick = null;
		}

		await ReleaseSpaceAsync();
		await base.DisposeAsyncCore();
	}

	// The bar is drawn anew each time it's shown, so the element watched can change while the bar stays visible.
	private async Task SyncSpaceAsync()
	{
		if (_binding)
		{
			return;
		}

		_binding = true;
		while (Visible && (_space is null || _spaceFor != _bar.Id))
		{
			await ReleaseSpaceAsync();
			string watched = _bar.Id;
			_space = await SafeModuleInvokeAsync<IJSObjectReference>(SpaceModule, "watchSpace", _bar, "--moka-action-bar-space", true);
			_spaceFor = watched;
			if (_space is null)
			{
				break;
			}
		}

		_binding = false;

		if (!Visible)
		{
			await ReleaseSpaceAsync();
		}
	}

	private async Task ReleaseSpaceAsync()
	{
		IJSObjectReference? space = _space;
		_space = null;
		_spaceFor = null;
		await space.SafeInvokeVoidAsync("dispose");
		await space.SafeDisposeAsync();
	}

	private async Task TickAsync()
	{
		if (_tick is null)
		{
			return;
		}

		_left = _endsAt - Clock.GetUtcNow();
		bool ended = _left <= TimeSpan.Zero;
		if (ended)
		{
			_left = TimeSpan.Zero;
			StopTicking();
		}

		StateHasChanged();
		if (ended)
		{
			await OnCountdownEnd.InvokeAsync();
		}
	}

	private void StopTicking()
	{
		_tick?.Dispose();
		_tick = null;
	}
}
