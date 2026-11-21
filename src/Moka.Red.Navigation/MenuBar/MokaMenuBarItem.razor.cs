using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.MenuBar;

/// <summary>
///     One command in a <see cref="MokaMenuBarMenu" />. Choosing it closes the menu and then runs the action, so
///     an action that opens a dialog does not fight the menu for focus.
/// </summary>
public partial class MokaMenuBarItem : MokaVisualComponentBase
{
	private ElementReference _itemRef;

	/// <summary>The menu this belongs to. Supplied by <see cref="MokaMenuBarMenu" />.</summary>
	[CascadingParameter]
	public MokaMenuBarMenu? Menu { get; set; }

	/// <summary>The item's label.</summary>
	[Parameter]
	[EditorRequired]
	public string? Text { get; set; }

	/// <summary>Icon before the label. Items without one still line up with those that have one.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Shortcut hint on the right, such as "Ctrl+N", drawn as one key cap per key. A label only: bind the key
	///     with <c>IMokaHotkeyService</c> and give its registration the same string.
	/// </summary>
	[Parameter]
	public string? Shortcut { get; set; }

	/// <summary>
	///     Draws the item in the error color, its icon too, on a red tint when it is hovered or highlighted, for a command
	///     such as Delete. The look only: ask before anything that can't be undone.
	/// </summary>
	[Parameter]
	public bool Danger { get; set; }

	/// <summary>Runs when the item is chosen, after the menu has closed.</summary>
	[Parameter]
	public EventCallback OnClick { get; set; }

	/// <summary>
	///     An address, so the item is a link (an <c>a</c> element with <c>role="menuitem"</c>): a middle click, Ctrl+click
	///     and the link's own context menu work. A click is the link's own; Enter or Space follow it from the menu.
	///     Choosing it still closes the menu and raises <see cref="OnClick" />, and from the keyboard an address in this
	///     tab is followed after <see cref="OnClick" />. A disabled item has nowhere to go, so it stays a disabled button,
	///     and a <c>javascript:</c>, <c>vbscript:</c> or <c>data:</c> address is not rendered: the item stays a button.
	/// </summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>
	///     Where the link opens, such as <c>_blank</c>. Only used with <see cref="Href" />. One that opens another tab gets
	///     <c>rel="noopener noreferrer"</c>, an external-link mark and <see cref="NewTabText" /> for screen readers, and
	///     Enter or Space open the tab before the menu closes and <see cref="OnClick" /> runs.
	/// </summary>
	[Parameter]
	public string? Target { get; set; }

	/// <summary>
	///     What screen readers hear after the name of a link whose <see cref="Target" /> opens another tab, after a comma.
	///     Default "opens in a new tab".
	/// </summary>
	[Parameter]
	public string NewTabText { get; set; } = "opens in a new tab";

	/// <inheritdoc />
	protected override string RootClass => "moka-menubar-item";

	/// <summary>The highlight is the menu's state, not a parameter.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-menubar-item--highlighted", Menu?.IsHighlighted(this) == true)
		.AddClass("moka-menubar-item--danger", Danger)
		.AddClass(Class)
		.Build();

	private bool OpensNewTab => FollowedHref is not null && UrlValues.OpensNewTab(Target);

	/// <summary>The address choosing the item goes to: none while it is disabled or the address could run script.</summary>
	internal string? FollowedHref => Disabled || string.IsNullOrEmpty(Href) ? null : UrlValues.SafeHref(Href);

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		Menu?.Register(this);
	}

	internal ValueTask FocusAsync() => SafeFocusAsync(_itemRef);

	internal async Task InvokeAsync()
	{
		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync();
		}
	}

	// A click on a link is the link's own: the browser follows it, and the menu only closes and runs the action.
	private async Task HandleClick()
	{
		if (Menu is not null)
		{
			await Menu.ChooseAsync(this, byKeyboard: false);
			return;
		}

		await InvokeAsync();
	}
}
