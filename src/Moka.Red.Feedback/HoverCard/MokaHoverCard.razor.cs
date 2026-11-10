using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Popover;

#pragma warning disable CA1849 // False positive: all Cancel calls use CancelAsync

namespace Moka.Red.Feedback.HoverCard;

/// <summary>
///     A rich content card that appears on hover, similar to GitHub user hover cards.
///     Wraps a trigger element and shows a floating card after a configurable delay.
///     Pure CSS/C# implementation - no JavaScript interop required.
/// </summary>
public partial class MokaHoverCard : MokaComponentBase
{
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	private readonly string _popupKey = $"moka-hover-card-{Guid.NewGuid():N}";
	private ElementReference _card;
	private CancellationTokenSource? _hideCts;
	private bool _isVisible;
	private CancellationTokenSource? _showCts;
	private string? _shownPopup;
	private ElementReference _trigger;

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

	private async Task HandleMouseEnter()
	{
		if (_hideCts is not null)
		{
			await _hideCts.CancelAsync();
			_hideCts = null;
		}

		if (_showCts is not null)
		{
			await _showCts.CancelAsync();
		}

		_showCts = new CancellationTokenSource();
		CancellationToken token = _showCts.Token;

		try
		{
			await Task.Delay(Delay, token);
			if (!token.IsCancellationRequested)
			{
				_isVisible = true;
				StateHasChanged();
			}
		}
		catch (TaskCanceledException)
		{
			// Expected when hover leaves before delay completes
		}
	}

	private async Task HandleMouseLeave()
	{
		if (_showCts is not null)
		{
			await _showCts.CancelAsync();
			_showCts = null;
		}

		if (_hideCts is not null)
		{
			await _hideCts.CancelAsync();
		}

		_hideCts = new CancellationTokenSource();
		CancellationToken token = _hideCts.Token;

		try
		{
			// Short grace period to allow moving to the card
			await Task.Delay(150, token);
			if (!token.IsCancellationRequested)
			{
				_isVisible = false;
				StateHasChanged();
			}
		}
		catch (TaskCanceledException)
		{
			// Expected when mouse re-enters before hide completes
		}
	}

	private async Task HandleCardMouseEnter()
	{
		if (_hideCts is not null)
		{
			await _hideCts.CancelAsync();
			_hideCts = null;
		}
	}

	private async Task HandleCardMouseLeave() => await HandleMouseLeave();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_shownPopup is not null)
		{
			_shownPopup = null;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _popupKey);
		}

		if (_showCts is not null)
		{
			await _showCts.CancelAsync();
			_showCts.Dispose();
		}

		if (_hideCts is not null)
		{
			await _hideCts.CancelAsync();
			_hideCts.Dispose();
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
