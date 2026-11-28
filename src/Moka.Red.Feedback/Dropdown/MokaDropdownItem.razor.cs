using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Dropdown;

/// <summary>
///     An individual item within a <see cref="MokaDropdown" />.
///     Supports text, icon, click handler, a link, a checked state, disabled and danger states, and divider mode.
/// </summary>
public partial class MokaDropdownItem : MokaComponentBase
{
	/// <summary>Custom content for the item. Overrides <see cref="Text" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Text label for the item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Optional icon displayed before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Click handler for the item. Enter and Space raise it too. Inside a
	///     <see cref="MokaDropdown" /> the menu has closed by the time it runs.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>
	///     Whether the item is disabled. The arrow keys skip a disabled item, and a link loses its address, so it goes
	///     nowhere.
	/// </summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>
	///     An address, so the item is a link (an <c>a</c> element with <c>role="menuitem"</c>). The browser's own link
	///     handling works: a middle click or Ctrl+click opens it in a new tab, the link's context menu offers to copy
	///     or open it, and a <c>download</c> attribute makes it a download. Choosing it still closes the dropdown and
	///     raises <see cref="OnClick" />. A <c>javascript:</c>, <c>vbscript:</c> or <c>data:</c> address is not
	///     rendered: the item stays a plain menu item.
	/// </summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>
	///     Where the link opens, such as <c>_blank</c>. Only used with <see cref="Href" />. One that opens another tab gets
	///     <c>rel="noopener noreferrer"</c>, an external-link mark and <see cref="NewTabText" /> for screen readers.
	/// </summary>
	[Parameter]
	public string? Target { get; set; }

	/// <summary>
	///     What screen readers hear after the name of a link whose <see cref="Target" /> opens another tab, after a comma.
	///     Default "opens in a new tab".
	/// </summary>
	[Parameter]
	public string NewTabText { get; set; } = "opens in a new tab";

	/// <summary>
	///     Draws the item in the error color, on a red tint when it is hovered or has keyboard focus, for an action
	///     such as Delete. The look only: ask before anything that can't be undone.
	/// </summary>
	[Parameter]
	public bool Danger { get; set; }

	/// <summary>
	///     Makes the item an option that is on or off, such as "Show grid": a <c>menuitemcheckbox</c> with
	///     <c>aria-checked</c>, and a check in the icon's place while it is on. When it is off the item shows its
	///     <see cref="Icon" />, or an empty space as wide as the check, so the names line up. The app keeps the state:
	///     flip it in <see cref="OnClick" />. Null, the default, is a plain menu item. A link (<see cref="Href" />) is
	///     never checkable, so it ignores this.
	/// </summary>
	[Parameter]
	public bool? Checked { get; set; }

	/// <summary>
	///     Makes a <see cref="Checked" /> item one choice of several, such as which view is open: a <c>menuitemradio</c>
	///     in place of a <c>menuitemcheckbox</c>, with the same check. Put the choices next to each other, with a
	///     <see cref="Divider" /> between them and other items. The app keeps exactly one of them checked. Ignored without
	///     <see cref="Checked" />.
	/// </summary>
	[Parameter]
	public bool Radio { get; set; }

	/// <summary>
	///     A keyboard shortcut drawn as key caps at the end of the row, such as <c>"Mod+S"</c>: one <c>MokaKbd</c> per key,
	///     as the context menu, the menu bar and the command palette draw theirs. A hint only: register the key with the
	///     hotkey registry to bind it.
	/// </summary>
	[Parameter]
	public string? Shortcut { get; set; }

	/// <summary>When true, renders as a horizontal divider instead of a menu item.</summary>
	[Parameter]
	public bool Divider { get; set; }

	[CascadingParameter] private MokaDropdown? Dropdown { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-dropdown-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-dropdown-item--disabled", Disabled)
		.AddClass("moka-dropdown-item--danger", Danger)
		.AddClass("moka-dropdown-item--checked", IsCheckable && Checked == true)
		.AddClass(Class)
		.Build();

	private string? LinkHref => string.IsNullOrEmpty(Href) ? null : UrlValues.SafeHref(Href);

	private bool IsLink => LinkHref is not null;

	private bool IsCheckable => Checked is not null && !IsLink;

	private string? CheckedState => IsCheckable ? (Checked == true ? "true" : "false") : null;

	private string Role => IsCheckable ? (Radio ? "menuitemradio" : "menuitemcheckbox") : "menuitem";

	private string CheckCssClass => new CssBuilder("moka-dropdown-item-icon")
		.AddClass("moka-dropdown-item-check")
		.AddClass("moka-dropdown-item-check--off", Checked != true)
		.Build();

	private bool OpensNewTab => IsLink && !Disabled && UrlValues.OpensNewTab(Target);

	// The rendered rel comes after the item's attributes and replaces theirs, so a rel the app gave stays for a link
	// that keeps to this tab.
	private string? LinkRel => UrlValues.RelFor(Target) ?? AdditionalAttributes?.GetValueOrDefault("rel") as string;

	private async Task HandleClick(MouseEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		if (Dropdown is not null)
		{
			await Dropdown.CloseForItemAsync();
		}

		await OnClick.InvokeAsync(e);
	}
}
