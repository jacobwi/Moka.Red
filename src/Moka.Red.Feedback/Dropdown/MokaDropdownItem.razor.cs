using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Dropdown;

/// <summary>
///     An individual item within a <see cref="MokaDropdown" />.
///     Supports text, icon, click handler, a link, disabled and danger states, and divider mode.
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
		.AddClass(Class)
		.Build();

	private string? LinkHref => string.IsNullOrEmpty(Href) ? null : UrlValues.SafeHref(Href);

	private bool IsLink => LinkHref is not null;

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
