using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.List;

/// <summary>
///     A single item in a <see cref="MokaList" />, supporting icon, text, secondary text,
///     end content, links, click handlers, and active/disabled states.
/// </summary>
public partial class MokaListItem
{
	/// <summary>Custom child content replacing or augmenting Text/SecondaryText.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Primary text for the list item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Secondary/description text below the primary text.</summary>
	[Parameter]
	public string? SecondaryText { get; set; }

	/// <summary>Leading icon.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Anything drawn in the leading icon's place, such as an emoji, a small picture or a <c>MokaIdenticon</c>, in the
	///     same box and spacing as <see cref="Icon" />, which it wins over, so a list of mixed items stays aligned.
	///     Pictures and SVG fill the box; text is set at the box's size. Hidden from screen readers like the icon, since
	///     <see cref="Text" /> names the item.
	/// </summary>
	[Parameter]
	public RenderFragment? IconContent { get; set; }

	/// <summary>
	///     The leading icon's color: a hex value, a color keyword or a color function such as <c>var()</c>. A value that
	///     is not a CSS color is ignored.
	/// </summary>
	[Parameter]
	public string? IconColor { get; set; }

	/// <summary>
	///     A color someone gave this item, such as a library or a folder they colored. <see cref="LabelMark" /> says how it
	///     shows: by default the icon, or whatever <see cref="IconContent" /> draws, takes the label's color, and an item
	///     with no icon gets a dot before its name. <see cref="IconColor" /> wins over the label on the icon. The shades
	///     follow the theme, light or dark, and <c>MokaTheme.WithLabelTone</c>.
	/// </summary>
	[Parameter]
	public MokaLabelColor? Label { get; set; }

	/// <summary>How <see cref="Label" /> shows. Default <see cref="MokaLabelMark.Icon" />.</summary>
	[Parameter]
	public MokaLabelMark LabelMark { get; set; } = MokaLabelMark.Icon;

	private bool HasIcon => IconContent is not null || Icon is not null;

	private bool LabelColorsIcon => Label is not null && LabelMark != MokaLabelMark.Dot && HasIcon;

	private bool ShowsLabelDot => Label is not null && (LabelMark == MokaLabelMark.Dot || !HasIcon);

	private string? IconColorValue => CssValues.IsColor(IconColor)
		? IconColor.Trim()
		: LabelColorsIcon ? MokaLabelColors.Solid(Label!.Value) : null;

	private string? IconStyle => new StyleBuilder()
		.AddStyle("color", IconColorValue)
		.Build();

	private string IconSlotStyle => new StyleBuilder()
		.AddStyle("--moka-icon-slot", MokaEnumHelpers.ToPixels(MokaSize.Sm))
		.AddStyle("color", IconColorValue)
		.Build()!;

	private string? LabelDotStyle => new StyleBuilder()
		.AddStyle("background-color", Label is { } label ? MokaLabelColors.Solid(label) : null)
		.Build();

	/// <summary>Trailing icon (shown when <see cref="EndContent" /> is null).</summary>
	[Parameter]
	public MokaIconDefinition? EndIcon { get; set; }

	/// <summary>Custom content rendered on the right side (badges, switches, etc.).</summary>
	[Parameter]
	public RenderFragment? EndContent { get; set; }

	/// <summary>Navigation URL. When set, renders as an anchor element.</summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>Click event callback.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>
	///     Right-click (context menu) callback. When attached, the browser's default context
	///     menu is suppressed. Pair with a context-menu service:
	///     <c>OnContextMenu="e =&gt; Menu.Show(e, ItemsForThisItem)"</c>.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnContextMenu { get; set; }

	/// <summary>Whether this item is in the active/selected state.</summary>
	[Parameter]
	public bool Active { get; set; }

	/// <summary>Shows a divider line after this item.</summary>
	[Parameter]
	public bool Divider { get; set; }

	/// <summary>Parent list reference for cascaded configuration.</summary>
	[CascadingParameter]
	public MokaList? ParentList { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-list-item";

	private string? LinkHref => UrlValues.SafeHref(Href);

	private bool IsLink => !string.IsNullOrEmpty(LinkHref);

	// A clickable row is a div, so it needs its own tab stop, and MokaList supplies Enter and
	// Space. Links are already focusable. A row with only a context menu still takes focus so
	// the keyboard menu key can open it. Outside a MokaList nothing would handle the keys, so
	// the row stays out of the tab order rather than taking focus and ignoring Enter.
	private bool IsInteractive => !IsLink && ParentList is not null
		&& (OnClick.HasDelegate || OnContextMenu.HasDelegate);

	private int? TabIndex => IsInteractive && !Disabled ? 0 : null;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-list-item--interactive", IsInteractive)
		.AddClass("moka-list-item--active", Active)
		.AddClass("moka-list-item--disabled", Disabled)
		.AddClass("moka-list-item--divider", Divider)
		.AddClass("moka-list-item--label-icon", LabelColorsIcon)
		.AddClass("moka-list-item--label-tinted", LabelColorsIcon && LabelMark == MokaLabelMark.TintedIcon)
		.AddClass(Class)
		.Build();

	// A link row sits inside a listitem wrapper, which is then the outermost element and takes the
	// margin. The row itself keeps the padding and the radius.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin, !IsLink)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private string? LinkWrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (IsInteractive && ParentList is not null)
		{
			await ParentList.EnableKeyboardActivationAsync();
		}
	}

	private async Task HandleClick(MouseEventArgs args)
	{
		if (!Disabled && OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private async Task HandleContextMenu(MouseEventArgs args)
	{
		if (!Disabled && OnContextMenu.HasDelegate)
		{
			await OnContextMenu.InvokeAsync(args);
		}
	}
}
