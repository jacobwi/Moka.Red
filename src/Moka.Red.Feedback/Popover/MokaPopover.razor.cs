using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Popover;

/// <summary>
///     A positioned popup that anchors to a trigger element.
///     Supports click, hover, and manual triggers with configurable positioning.
///     More flexible than Tooltip — supports any content and stays open for interaction.
/// </summary>
public partial class MokaPopover : MokaVisualComponentBase
{
	private bool _hoverIntent;
	private bool _open;

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

	/// <inheritdoc />
	protected override string RootClass => "moka-popover";

	private string WrapperCss => new CssBuilder("moka-popover-wrapper")
		.AddClass(Class)
		.Build();

	private string PopupCss => new CssBuilder("moka-popover-popup")
		.AddClass($"moka-popover-popup--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass("moka-popover-popup--arrow", Arrow)
		.AddClass("moka-popover-popup--match-width", MatchWidth)
		.Build();

	private string? PopupStyle => new StyleBuilder()
		.AddStyle("--moka-popover-offset-x", $"{OffsetX}px", OffsetX != 0)
		.AddStyle("--moka-popover-offset-y", $"{OffsetY}px", OffsetY != 4)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (_open != Open)
		{
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
		Open = true;

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
		Open = false;

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}
	}
}
