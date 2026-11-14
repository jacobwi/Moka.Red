using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.HoverCard;

/// <summary>
///     A rich content card that appears on hover, similar to GitHub user hover cards.
///     Wraps a trigger element and shows a floating card after a configurable delay.
///     Pure CSS/C# implementation - no JavaScript interop required.
/// </summary>
public partial class MokaHoverCard : MokaComponentBase
{
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	// Long enough to move the pointer from the trigger onto the card.
	private static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(150);

	private readonly string _popupKey = $"moka-hover-card-{Guid.NewGuid():N}";
	private ElementReference _card;
	private ITimer? _hideTimer;
	private int _hideWait;
	private bool _isVisible;
	private ITimer? _showTimer;
	private int _showWait;
	private string? _shownPopup;
	private ElementReference _trigger;

	// Optional, so looked up rather than injected: [Inject] throws when a service is missing.
	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	// An app can register its own clock. A test does, so no test waits on a real hover delay.
	private TimeProvider Clock => Services.GetService<TimeProvider>() ?? TimeProvider.System;

	/// <summary>Trigger element that activates the hover card on hover.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Content rendered inside the hover card body.</summary>
	[Parameter]
	public RenderFragment? CardContent { get; set; }

	/// <summary>Position of the card relative to the trigger. Default is <see cref="MokaPopoverPosition.Bottom" />.</summary>
	[Parameter]
	public MokaPopoverPosition Position { get; set; } = MokaPopoverPosition.Bottom;

	/// <summary>Delay in milliseconds before showing the card. Default is 300.</summary>
	[Parameter]
	public int Delay { get; set; } = 300;

	/// <summary>Maximum width of the hover card. Default is "320px".</summary>
	[Parameter]
	public string MaxWidth { get; set; } = "320px";

	/// <summary>
	///     How the card is drawn. Null takes the theme's popup look (<see cref="MokaTheme.WithPopups" />), which reaches
	///     every floating panel through the provider's root. A value here wins over the theme. Only the new design draws
	///     the looks: under <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" /> the card looks the same
	///     whatever this says.
	/// </summary>
	[Parameter]
	public MokaPopupLook? Look { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-hover-card";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-hover-card--{PositionToKebab(Position)}")
		.AddClass("moka-hover-card--visible", _isVisible)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	private string CardClass => new CssBuilder("moka-hover-card__card")
		.AddClass(Look is { } look ? $"moka-popups-{MokaEnumHelpers.ToCssClass(look)}" : null)
		.Build();

	private string CardStyle => new StyleBuilder()
		.AddStyle("max-width", MaxWidth)
		.Build() ?? "";

	// What moka-popup.js was last told: a render that changes none of it sends nothing.
	private string? PopupState => _isVisible && CardContent is not null ? PositionToKebab(Position) : null;

	/// <summary>Has internal visibility state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The card goes to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		string? popup = PopupState;
		if (popup != _shownPopup)
		{
			_shownPopup = popup;
			if (popup is null)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _popupKey);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _popupKey, _card, _trigger, null,
					new Dictionary<string, object?> { ["placement"] = popup });
			}
		}
	}

	private void HandleMouseEnter()
	{
		StopWaiting(ref _hideTimer, ref _hideWait);
		int wait = StopWaiting(ref _showTimer, ref _showWait);
		if (Delay == 0)
		{
			_isVisible = true;
			return;
		}

		_showTimer = Clock.CreateTimer(
			_ => RunDetached(() => Settle(wait == _showWait, visible: true)),
			null,
			TimeSpan.FromMilliseconds(Delay),
			Timeout.InfiniteTimeSpan);
	}

	private void HandleMouseLeave()
	{
		StopWaiting(ref _showTimer, ref _showWait);
		int wait = StopWaiting(ref _hideTimer, ref _hideWait);
		_hideTimer = Clock.CreateTimer(
			_ => RunDetached(() => Settle(wait == _hideWait, visible: false)),
			null,
			HideDelay,
			Timeout.InfiniteTimeSpan);
	}

	private void HandleCardMouseEnter() => StopWaiting(ref _hideTimer, ref _hideWait);

	private void HandleCardMouseLeave() => HandleMouseLeave();

	// The pointer routinely leaves before a wait runs out, so a wait is cut short by disposing its timer, which
	// throws nothing. That doesn't stop a callback that already fired and is waiting for the renderer, so each
	// wait carries the number it started under and only the latest one changes anything.
	private static int StopWaiting(ref ITimer? timer, ref int wait)
	{
		timer?.Dispose();
		timer = null;
		return ++wait;
	}

	private void Settle(bool latest, bool visible)
	{
		if (!latest)
		{
			return;
		}

		_isVisible = visible;
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		StopWaiting(ref _showTimer, ref _showWait);
		StopWaiting(ref _hideTimer, ref _hideWait);

		if (_shownPopup is not null)
		{
			_shownPopup = null;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _popupKey);
		}

		await base.DisposeAsyncCore();
	}

	private static string PositionToKebab(MokaPopoverPosition position) => position switch
	{
		MokaPopoverPosition.Top => "top",
		MokaPopoverPosition.Bottom => "bottom",
		MokaPopoverPosition.Left => "left",
		MokaPopoverPosition.Right => "right",
		MokaPopoverPosition.TopStart => "top-start",
		MokaPopoverPosition.TopEnd => "top-end",
		MokaPopoverPosition.BottomStart => "bottom-start",
		MokaPopoverPosition.BottomEnd => "bottom-end",
		_ => "bottom"
	};
}
