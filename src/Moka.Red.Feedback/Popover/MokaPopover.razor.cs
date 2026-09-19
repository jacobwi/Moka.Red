using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Popover;

/// <summary>
///     A positioned popup that anchors to a trigger element.
///     Supports click, hover, and manual triggers with configurable positioning.
///     More flexible than Tooltip: it takes any content and stays open for interaction.
/// </summary>
public partial class MokaPopover : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/Popover/MokaPopover.razor.js";

	private readonly string _popupId = $"moka-popover-{Guid.NewGuid():N}";
	private bool _hoverIntent;
	private bool _lastOpenParameter;
	private bool _open;
	private bool? _syncedOpen;
	private ElementReference _triggerArea;

	/// <summary>The trigger element.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The popup content displayed inside the popover.</summary>
	[Parameter]
	public RenderFragment? PopoverContent { get; set; }

	/// <summary>Whether the popover is currently visible. Two-way bindable.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>How the popover is triggered. Defaults to <see cref="MokaPopoverTrigger.Click" />.</summary>
	[Parameter]
	public MokaPopoverTrigger Trigger { get; set; } = MokaPopoverTrigger.Click;

	/// <summary>Position relative to the trigger. Defaults to <see cref="MokaPopoverPosition.Bottom" />.</summary>
	[Parameter]
	public MokaPopoverPosition Position { get; set; } = MokaPopoverPosition.Bottom;

	/// <summary>Whether clicking outside the popover closes it. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnClickOutside { get; set; } = true;

	/// <summary>Whether pressing Escape closes the popover. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>Horizontal offset in pixels. Defaults to 0.</summary>
	[Parameter]
	public int OffsetX { get; set; }

	/// <summary>Vertical offset in pixels (gap from anchor). Defaults to 4.</summary>
	[Parameter]
	public int OffsetY { get; set; } = 4;

	/// <summary>Whether to show an arrow/caret pointing to the anchor. Defaults to false.</summary>
	[Parameter]
	public bool Arrow { get; set; }

	/// <summary>Whether the popover matches the trigger width (useful for dropdowns). Defaults to false.</summary>
	[Parameter]
	public bool MatchWidth { get; set; }

	/// <summary>
	///     Accessible name of the popup. Without it the popup is named by the trigger's text.
	/// </summary>
	[Parameter]
	public string? AriaLabel { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-popover";

	private string TriggerId => $"{_popupId}-trigger";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private string PopupCss => new CssBuilder("moka-popover-popup")
		.AddClass($"moka-popover-popup--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass("moka-popover-popup--arrow", Arrow)
		.AddClass("moka-popover-popup--match-width", MatchWidth)
		.Build();

	// The wrapper sits in the page around the trigger, so it takes the margin. The popup is the
	// panel the popover draws, so it takes the padding and radius.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? PopupStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		// Invariant: Swedish writes a negative number with U+2212, which CSS does not read.
		.AddStyle("--moka-popover-offset-x", FormattableString.Invariant($"{OffsetX}px"), OffsetX != 0)
		.AddStyle("--moka-popover-offset-y", FormattableString.Invariant($"{OffsetY}px"), OffsetY != 4)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The trigger is the consumer's markup, so its aria-expanded is written in the browser, once
		// at the start and again whenever the popup opens or closes.
		if (_syncedOpen != _open)
		{
			_syncedOpen = _open;
			await SafeModuleInvokeVoidAsync(ModulePath, "syncTrigger", _triggerArea, _open, _popupId);
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Open moves the popover only when the parent passes a new value. A re-render that passes the
		// old one again must not undo what the user did (gotcha #9).
		if (Open != _lastOpenParameter)
		{
			_lastOpenParameter = Open;
			_open = Open;
		}
	}

	private async Task HandleTriggerClick()
	{
		if (Trigger != MokaPopoverTrigger.Click)
		{
			return;
		}

		if (_open)
		{
			await CloseAsync();
		}
		else
		{
			await OpenAsync();
		}
	}

	private async Task HandleMouseEnter()
	{
		if (Trigger != MokaPopoverTrigger.Hover)
		{
			return;
		}

		_hoverIntent = true;
		await OpenAsync();
	}

	private async Task HandleMouseLeave()
	{
		if (Trigger != MokaPopoverTrigger.Hover)
		{
			return;
		}

		_hoverIntent = false;

		// Small delay to allow moving to the popover content
		await Task.Delay(100);
		if (!_hoverIntent)
		{
			await CloseAsync();
		}
	}

	// While the popover is open and Escape closes it, keys stop at the popover, so a MokaDialog
	// around it does not close on the same Escape.
	private bool StopsEscape => _open && CloseOnEscape;

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (CloseOnEscape && e.Key == "Escape" && _open)
		{
			await CloseAsync();
		}
	}

	private async Task HandleBackdropClick()
	{
		if (CloseOnClickOutside)
		{
			await CloseAsync();
		}
	}

	private async Task OpenAsync()
	{
		if (_open)
		{
			return;
		}

		_open = true;

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(true);
		}
	}

	private async Task CloseAsync()
	{
		if (!_open)
		{
			return;
		}

		_open = false;

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}
	}
}
