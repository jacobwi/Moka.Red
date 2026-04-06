using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.Dropdown;

/// <summary>
///     A dropdown menu component that displays action items when triggered.
///     Uses <see cref="MokaPopover" /> internally with click trigger.
///     Simpler than ContextMenu for common dropdown patterns.
/// </summary>
public partial class MokaDropdown : MokaVisualComponentBase
{
	/// <summary>The trigger element (usually a button).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The dropdown content (menu items).</summary>
	[Parameter]
	public RenderFragment? Items { get; set; }

	/// <summary>Whether the dropdown is currently visible. Two-way bindable.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>Position relative to the trigger. Defaults to <see cref="MokaPopoverPosition.BottomStart" />.</summary>
	[Parameter]
	public MokaPopoverPosition Position { get; set; } = MokaPopoverPosition.BottomStart;

	/// <summary>Whether clicking a menu item closes the dropdown. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnItemClick { get; set; } = true;

	/// <summary>Whether the dropdown matches the trigger width. Defaults to false.</summary>
	[Parameter]
	public bool MatchWidth { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-dropdown";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleOpenChanged(bool open)
	{
		Open = open;
		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(open);
		}
	}

	private async Task HandleItemClick()
	{
		if (CloseOnItemClick)
		{
			Open = false;
			if (OpenChanged.HasDelegate)
			{
				await OpenChanged.InvokeAsync(false);
			}
		}
	}
}
