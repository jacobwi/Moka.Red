using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Interactions;
using Moka.Red.Core.Utilities;
using Moka.Red.Primitives.Utility;

namespace Moka.Red.Primitives.DataList;

/// <summary>
///     A definition list component for displaying structured key-value data.
///     Renders as a semantic &lt;dl&gt; element with &lt;dt&gt; and &lt;dd&gt; pairs.
///     Supports both data-driven (via <see cref="Items" />) and template-driven
///     (via <see cref="ChildContent" />) usage.
/// </summary>
public partial class MokaDataList : MokaVisualComponentBase
{
	/// <summary>
	///     What an empty value reads under <see cref="MokaDataListLook.Terminal" /> when <see cref="EmptyText" /> is null.
	/// </summary>
	public const string DefaultTerminalEmptyText = "not set";

	/// <summary>The data items to display. Each item renders as a dt/dd pair.</summary>
	[Parameter]
	public IReadOnlyList<MokaDataListItem>? Items { get; set; }

	/// <summary>
	///     Manual child content as an alternative to <see cref="Items" />: each pair a <c>&lt;div&gt;</c> holding a
	///     <c>&lt;dt&gt;</c> and a <c>&lt;dd&gt;</c>, which take the list's styles.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Layout orientation. Column (default) stacks terms vertically; Row displays them inline, and a value
	///     moves under its term when there isn't room beside it. Only <see cref="MokaDataListLook.Standard" /> reads it:
	///     the other looks arrange their pairs themselves.
	/// </summary>
	[Parameter]
	public MokaDirection Orientation { get; set; } = MokaDirection.Column;

	/// <summary>
	///     How the list draws its pairs. <see cref="MokaDataListLook.Standard" /> (the default) keeps the list as it was.
	///     A look sets its own type sizes, so <c>Size</c> applies to Standard only.
	/// </summary>
	[Parameter]
	public MokaDataListLook Look { get; set; }

	/// <summary>
	///     How the terms weigh against their values. <see cref="MokaDataListTermStyle.Standard" /> (the default) keeps the
	///     look's own term; Fact tiles and Terminal ignore it.
	/// </summary>
	[Parameter]
	public MokaDataListTermStyle TermStyle { get; set; }

	/// <summary>
	///     How a copyable value shows it can be copied. <see cref="MokaDataListCopyStyle.Faint" /> (the default) is a quiet
	///     button that comes up when its row is hovered.
	/// </summary>
	[Parameter]
	public MokaDataListCopyStyle CopyStyle { get; set; }

	/// <summary>Whether to apply alternating row backgrounds.</summary>
	[Parameter]
	public bool Striped { get; set; }

	/// <summary>Whether to show borders between items.</summary>
	[Parameter]
	public bool Bordered { get; set; }

	/// <summary>Whether to use compact spacing.</summary>
	[Parameter]
	public bool Dense { get; set; }

	/// <summary>
	///     Shown in a muted color for an item with nothing to show, such as "Not set". Null (the default) leaves
	///     the value empty, except under <see cref="MokaDataListLook.Terminal" />, which reads
	///     <see cref="DefaultTerminalEmptyText" />.
	/// </summary>
	[Parameter]
	public string? EmptyText { get; set; }

	/// <summary>
	///     The width of every term in a <see cref="MokaDirection.Row" /> list, such as <c>10rem</c>, so the values
	///     line up in one column; a longer term wraps. A length or a <c>var()</c>. Null keeps each term at least
	///     8rem and as wide as its text. Settings rows, Inset panel, Spec sheet and Terminal read it as their term
	///     column; Fact tiles have none.
	/// </summary>
	[Parameter]
	public string? TermWidth { get; set; }

	/// <summary>
	///     Lays the items out in up to this many columns, as many as fit at <see cref="MinColumn" /> each, so a
	///     narrow list still gets one. Default 1.
	/// </summary>
	[Parameter]
	public int Columns { get; set; } = 1;

	/// <summary>
	///     The narrowest a column gets when <see cref="Columns" /> is above 1, and the narrowest a fact tile gets. A length
	///     or a <c>var()</c>. Default 16rem, and 11rem for tiles.
	/// </summary>
	[Parameter]
	public string? MinColumn { get; set; }

	/// <summary>
	///     Raised when an item's row is right-clicked, with the item and the mouse event, so a context menu can
	///     open at the pointer. The browser's own menu is cancelled only while this has a listener.
	/// </summary>
	[Parameter]
	public EventCallback<MokaItemContextMenuArgs<MokaDataListItem>> OnItemContextMenu { get; set; }

	/// <summary>
	///     Names a copy button and its tooltip. Defaults to "Copy " and the item's <see cref="MokaDataListItem.Label" />.
	/// </summary>
	[Parameter]
	public Func<MokaDataListItem, string>? CopyLabel { get; set; }

	/// <summary>Shown above a copy button after a copy. Default "Copied".</summary>
	[Parameter]
	public string CopiedText { get; set; } = "Copied";

	/// <summary>Shown above a copy button when the browser refused the copy. Default "Couldn't copy".</summary>
	[Parameter]
	public string CopyFailedText { get; set; } = MokaCopyButton.DefaultFailedText;

	private bool HasColumns => Columns > 1 && Look != MokaDataListLook.Inline;

	private bool IsTiles => Look == MokaDataListLook.FactTiles;

	private bool IsInline => Look == MokaDataListLook.Inline;

	private string? ResolvedTermWidth => CssValues.IsLengthOrVariable(TermWidth) ? CssValues.WithPixels(TermWidth.Trim()) : null;

	private string? ResolvedMinColumn => CssValues.IsLengthOrVariable(MinColumn) ? CssValues.WithPixels(MinColumn.Trim()) : null;

	private string? ResolvedEmptyText => EmptyText ?? (Look == MokaDataListLook.Terminal ? DefaultTerminalEmptyText : null);

	// Settings rows and the inset panel draw lines between rows as part of their look, and Bordered's rules already
	// keep those lines right in columns.
	private bool DrawsLines => (Bordered && !IsInline) || Look is MokaDataListLook.SettingsRows or MokaDataListLook.InsetPanel;

	private string? LookClass => Look switch
	{
		MokaDataListLook.SettingsRows => "moka-data-list--settings",
		MokaDataListLook.InsetPanel => "moka-data-list--inset",
		MokaDataListLook.FactTiles => "moka-data-list--tiles",
		MokaDataListLook.SpecSheet => "moka-data-list--spec",
		MokaDataListLook.Terminal => "moka-data-list--terminal",
		MokaDataListLook.Inline => "moka-data-list--inline",
		_ => null
	};

	private string? TermStyleClass => Look is MokaDataListLook.FactTiles or MokaDataListLook.Terminal or MokaDataListLook.Inline
		? null
		: TermStyle switch
		{
			MokaDataListTermStyle.Quiet => "moka-data-list--term-quiet",
			MokaDataListTermStyle.Micro => "moka-data-list--term-micro",
			MokaDataListTermStyle.Strong => "moka-data-list--term-strong",
			_ => null
		};

	private string? CopyStyleClass => CopyStyle switch
	{
		MokaDataListCopyStyle.Always => "moka-data-list--copy-always",
		MokaDataListCopyStyle.ClickValue => "moka-data-list--copy-value",
		_ => null
	};

	/// <inheritdoc />
	protected override string RootClass => "moka-data-list";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-data-list--{SizeToKebab(Size)}")
		.AddClass("moka-data-list--row", Orientation == MokaDirection.Row && Look == MokaDataListLook.Standard)
		.AddClass("moka-data-list--striped", Striped && !IsInline)
		.AddClass("moka-data-list--bordered", DrawsLines)
		.AddClass("moka-data-list--dense", Dense)
		.AddClass("moka-data-list--term-width", ResolvedTermWidth is not null)
		.AddClass("moka-data-list--columns", HasColumns)
		.AddClass("moka-data-list--disabled", Disabled)
		.AddClass(LookClass)
		.AddClass(TermStyleClass)
		.AddClass(CopyStyleClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--_dl-term-width", ResolvedTermWidth)
		.AddStyle("--_dl-columns", Columns.ToString(CultureInfo.InvariantCulture), HasColumns)
		.AddStyle("--_dl-min-column", ResolvedMinColumn, HasColumns || IsTiles)
		.AddStyle(Style)
		.Build();

	private string EntryClass(MokaDataListItem item, string? copy) => new CssBuilder("moka-data-list-entry")
		.AddClass("moka-quiet-copies", (copy is not null || item.CopiesSeveral) && CopyStyle == MokaDataListCopyStyle.Faint)
		.AddClass("moka-data-list-entry--wide", item.Wide)
		.AddClass(item.Class)
		.Build();

	private static string HelpClass(MokaDataListItem item) => new CssBuilder("moka-data-list-help")
		.AddClass("moka-about", !item.AlwaysShowHelp)
		.Build();

	private static string TermClass(MokaIconDefinition? icon) => new CssBuilder("moka-data-list-term")
		.AddClass("moka-data-list-term--icon", icon is not null)
		.Build();

	// The terminal is keys and values only, so it leaves out an item's icon and help. A line of pairs has no room for a
	// line of help under a term.
	private MokaIconDefinition? IconOf(MokaDataListItem item) => Look == MokaDataListLook.Terminal ? null : item.Icon;

	private bool HasHelp(MokaDataListItem item) =>
		Look is not (MokaDataListLook.Terminal or MokaDataListLook.Inline) && !string.IsNullOrEmpty(item.Help);

	private string DescriptionClass(MokaDataListItem item, bool empty) => new CssBuilder("moka-data-list-description")
		.AddClass("moka-data-list-description--mono", item.Mono)
		.AddClass("moka-data-list-description--empty", empty && ResolvedEmptyText is not null)
		.AddClass("moka-data-list-description--end", item.EndContent is not null)
		.AddClass("moka-data-list-description--values", item.HasSeveral)
		.Build();

	private string CopyName(MokaDataListItem item) =>
		CopyLabel?.Invoke(item) ?? $"{MokaCopyButton.DefaultTooltipText} {item.Label}";

	// Several buttons in one row would all read "Copy Addresses", so each says its value too.
	private string ValueCopyName(MokaDataListItem item, MokaDataListValue value, string shown) =>
		value.CopyLabel ?? $"{CopyName(item)}, {shown}";

	private static RenderFragment Plain(string text) => builder => builder.AddContent(0, text);

	private static string ShownText(MokaDataListItem item, string copy) => string.IsNullOrEmpty(item.Value) ? copy : item.Value;

	private bool ListensForContextMenu => OnItemContextMenu.HasDelegate && !Disabled;

	private EventCallback<MouseEventArgs> ContextMenuHandler(MokaDataListItem item) => ListensForContextMenu
		? EventCallback.Factory.Create<MouseEventArgs>(this,
			e => OnItemContextMenu.InvokeAsync(new MokaItemContextMenuArgs<MokaDataListItem>(item, e)))
		: default;

	private RenderFragment ValueOf(MokaDataListItem item, string? copy, bool empty) => builder =>
	{
		if (item.Values is not null)
		{
			builder.AddContent(13, ResolvedEmptyText);
		}
		else if (copy is not null && CopyStyle == MokaDataListCopyStyle.ClickValue)
		{
			string name = CopyName(item);
			string shown = ShownText(item, copy);
			builder.AddContent(12, CopyValue(name, $"{name}, {shown}", copy, item.ValueContent ?? Plain(shown)));
		}
		else if (copy is not null)
		{
			builder.OpenComponent<MokaCopyable>(0);
			builder.AddComponentParameter(1, nameof(MokaCopyable.Text), copy);
			builder.AddComponentParameter(2, nameof(MokaCopyable.Label), item.Label);
			builder.AddComponentParameter(3, nameof(MokaCopyable.Mono), false);
			builder.AddComponentParameter(4, nameof(MokaCopyable.Wrap), true);
			builder.AddComponentParameter(5, nameof(MokaCopyable.CopyLabel), CopyLabel?.Invoke(item));
			builder.AddComponentParameter(6, nameof(MokaCopyable.CopiedText), CopiedText);
			builder.AddComponentParameter(7, nameof(MokaCopyable.FailedText), CopyFailedText);
			builder.AddComponentParameter(8, nameof(MokaCopyable.ChildContent),
				item.ValueContent ?? (shown => shown.AddContent(0, string.IsNullOrEmpty(item.Value) ? copy : item.Value)));
			builder.CloseComponent();
		}
		else if (item.ValueContent is not null)
		{
			builder.AddContent(9, item.ValueContent);
		}
		else if (empty && ResolvedEmptyText is not null)
		{
			builder.AddContent(10, ResolvedEmptyText);
		}
		else
		{
			builder.AddContent(11, item.Value);
		}
	};
}
