using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.Dropdown;

/// <summary>
///     A dropdown menu component that displays action items when triggered.
///     Uses <see cref="MokaPopover" /> internally with click trigger.
///     Simpler than ContextMenu for common dropdown patterns.
/// </summary>
/// <remarks>
///     Follows the WAI-ARIA menu button pattern. The first focusable element in
///     <see cref="ChildContent" /> is the menu button: it gets <c>aria-haspopup="menu"</c> and
///     <c>aria-expanded</c>. Enter, Space or Down Arrow open the menu on its first item, Up Arrow
///     on its last. In the menu the arrow keys, Home and End move between items, Enter and Space
///     activate one, Escape closes the menu and returns focus to the button, and Tab closes it.
/// </remarks>
public partial class MokaDropdown : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/Dropdown/MokaDropdown.razor.js";

	private readonly string _generatedId = $"moka-dropdown-{Guid.NewGuid():N}";

	// The rendered state. The Open parameter only seeds it when the parent passes a new value, so
	// a parent re-render with the same one-way value does not close a menu the user opened.
	private bool _lastOpenParameter;
	private bool _open;
	private ElementReference _root;

	/// <summary>The trigger element (usually a button). Its first focusable element is the menu button.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The dropdown content (menu items).</summary>
	[Parameter]
	public RenderFragment? Items { get; set; }

	/// <summary>
	///     Whether the dropdown is currently visible. Two-way bindable. A one-way value opens or
	///     closes the menu only when it changes.
	/// </summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>Position relative to the trigger. Defaults to <see cref="MokaPopoverPosition.BottomStart" />.</summary>
	[Parameter]
	public MokaPopoverPosition Position { get; set; } = MokaPopoverPosition.BottomStart;

	/// <summary>
	///     Whether clicking a menu item closes the dropdown. Defaults to true. The menu closes
	///     before the item's <see cref="MokaDropdownItem.OnClick" /> runs.
	/// </summary>
	[Parameter]
	public bool CloseOnItemClick { get; set; } = true;

	/// <summary>Whether the dropdown matches the trigger width. Defaults to false.</summary>
	[Parameter]
	public bool MatchWidth { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-dropdown";

	private string MenuId => $"{Id ?? _generatedId}-menu";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (Open != _lastOpenParameter)
		{
			_lastOpenParameter = Open;
			_open = Open;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The trigger is the consumer's markup, so its ARIA state, the arrow keys and the focus
		// moves are handled in the browser.
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "bindDropdown", _root);
		}
	}

	/// <summary>
	///     Closes the menu when <see cref="CloseOnItemClick" /> is set. Items call this before they
	///     run their action: an action that opens a dialog would otherwise leave the menu open
	///     behind it until the dialog was done.
	/// </summary>
	internal async Task CloseForItemAsync()
	{
		if (!CloseOnItemClick || !_open)
		{
			return;
		}

		_open = false;
		StateHasChanged();
		await OpenChanged.InvokeAsync(false);
	}

	private async Task HandleOpenChanged(bool open)
	{
		_open = open;
		await OpenChanged.InvokeAsync(open);
	}

	// A MokaDropdownItem has closed the menu by now. This covers other content in Items.
	private Task HandleMenuClick() => CloseForItemAsync();
}
