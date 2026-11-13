using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Navigation.Breadcrumb;

/// <summary>
///     A breadcrumb navigation trail. Renders child <see cref="MokaBreadcrumbItem" /> elements
///     separated by configurable separators.
/// </summary>
/// <remarks>
///     The breadcrumb decides where every level goes: which is the current page, which fold into the menu and where
///     separators fall. Levels count in the order the markup gives them, and each one renders again whenever a level
///     comes, goes or changes, so a trail built at runtime stays right.
/// </remarks>
public partial class MokaBreadcrumb
{
	/// <summary>Default <see cref="MoreLabel" />.</summary>
	public const string DefaultMoreLabel = "Show {n} more levels";

	/// <summary>Default <see cref="ItemMaxWidth" />.</summary>
	public const string DefaultItemMaxWidth = "12rem";

	private static readonly (int First, int Last) NoRange = (0, -1);

	private readonly List<MokaBreadcrumbItem> _items = [];
	private bool _disposing;
	private int _generation;
	private bool _narrowOpen;
	private bool _rebuilding;
	private bool _wideOpen;

	/// <summary>Child breadcrumb items.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Text separator between items. Null, the default, is a slash on the original design and a chevron on the new
	///     one; a path always takes a slash.
	/// </summary>
	[Parameter]
	public string? Separator { get; set; }

	/// <summary>Custom separator content. Overrides <see cref="Separator" /> text when set.</summary>
	[Parameter]
	public RenderFragment? SeparatorContent { get; set; }

	/// <summary>
	///     When the trail has more levels than this, the middle ones fold into a menu behind a "···" button: the first
	///     level and the last <c>MaxItems - 1</c> stay. At 1 only the current page stays. Counts the current page even
	///     when <see cref="ShowCurrent" /> is off. A path ignores it.
	/// </summary>
	[Parameter]
	public int? MaxItems { get; set; }

	/// <summary>
	///     Whether the last level, the page the person is on, shows. Defaults to true. Turn it off in a page header,
	///     where the title says it.
	/// </summary>
	[Parameter]
	public bool ShowCurrent { get; set; } = true;

	/// <summary>
	///     The name and tooltip of the "···" button. <c>{n}</c> is replaced with the number of folded levels. Defaults
	///     to <see cref="DefaultMoreLabel" />, which says "level" for one.
	/// </summary>
	[Parameter]
	public string MoreLabel { get; set; } = DefaultMoreLabel;

	/// <summary>
	///     Folds the middle levels into the menu on its own when the breadcrumb's box is narrow, with a container query
	///     and no script. Folding needs a box whose width doesn't come from the trail, so the breadcrumb then takes the
	///     width it's given. Defaults to <see cref="MokaBreadcrumbFold.Never" />.
	/// </summary>
	[Parameter]
	public MokaBreadcrumbFold Fold { get; set; }

	/// <summary>
	///     How wide one level's name may get on the new design before it's cut, with the whole name in a tooltip. A CSS
	///     length, default 12rem. Set it to cut names on the original design too.
	/// </summary>
	[Parameter]
	public string ItemMaxWidth { get; set; } = DefaultItemMaxWidth;

	/// <summary>How the levels are drawn. Defaults to <see cref="MokaBreadcrumbLook.Trail" />.</summary>
	[Parameter]
	public MokaBreadcrumbLook Look { get; set; }

	/// <summary>
	///     Whether a trail too long for its box wraps onto more lines. Defaults to true. Turned off, the trail stays on one
	///     line that keeps its end, the current page, in view and fades its start, scrolling sideways as a path does. A
	///     path never wraps.
	/// </summary>
	[Parameter]
	public bool Wrap { get; set; } = true;

	/// <summary>Content before the first level, such as a host or an environment chip in a path.</summary>
	[Parameter]
	public RenderFragment? Root { get; set; }

	/// <summary>Whether a copy button at the end copies the path. Made for <see cref="MokaBreadcrumbLook.Path" />.</summary>
	[Parameter]
	public bool Copyable { get; set; }

	/// <summary>
	///     What the copy button copies. Null joins every level's <see cref="MokaBreadcrumbItem.Segment" />, or its
	///     text, with "/".
	/// </summary>
	[Parameter]
	public string? CopyText { get; set; }

	/// <summary>The copy button's name and tooltip. Defaults to "Copy path".</summary>
	[Parameter]
	public string CopyLabel { get; set; } = "Copy path";

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-breadcrumb";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(IsTrail ? "moka-breadcrumb--trail" : "moka-breadcrumb--path")
		.AddClass("moka-breadcrumb--fold-narrow", IsTrail && Fold == MokaBreadcrumbFold.Narrow)
		.AddClass("moka-breadcrumb--fold-medium", IsTrail && Fold == MokaBreadcrumbFold.Medium)
		.AddClass("moka-breadcrumb--item-max", HasOwnItemMaxWidth)
		.AddClass("moka-breadcrumb--has-back", BackLevel is not null)
		.AddClass("moka-breadcrumb--one-line", OneLine)
		.AddClass(SizeClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--_breadcrumb-item-max", ItemMaxWidth, HasOwnItemMaxWidth)
		.AddStyle(Style)
		.Build();

	private bool IsTrail => Look == MokaBreadcrumbLook.Trail;

	private bool OneLine => IsTrail && !Wrap;

	// A path's height follows Size on the new design. Md keeps the height a path has always had, so it writes nothing.
	private string? SizeClass => !IsTrail && Size != MokaSize.Md ? $"moka-breadcrumb--{SizeToKebab(Size)}" : null;

	private bool HasOwnItemMaxWidth =>
		!string.IsNullOrWhiteSpace(ItemMaxWidth) && !string.Equals(ItemMaxWidth, DefaultItemMaxWidth, StringComparison.Ordinal);

	// The levels MaxItems folds, whatever the width.
	private (int First, int Last) AlwaysFolded
	{
		get
		{
			int count = _items.Count;
			if (!IsTrail || MaxItems is not int max)
			{
				return NoRange;
			}

			max = Math.Max(1, max);
			if (count <= max)
			{
				return NoRange;
			}

			return max == 1 ? (0, count - 2) : (1, count - max);
		}
	}

	// The levels folded in a narrow box: the first and the last two stay, and MaxItems may fold more.
	private (int First, int Last) NarrowFolded
	{
		get
		{
			(int first, int last) = AlwaysFolded;
			int count = _items.Count;
			if (!IsTrail || Fold == MokaBreadcrumbFold.Never || count <= 3)
			{
				return (first, last);
			}

			return first > last ? (1, count - 3) : (Math.Min(first, 1), Math.Max(last, count - 3));
		}
	}

	private bool FoldsWhenNarrow => NarrowFolded != AlwaysFolded;

	// The level each fold button sits in front of.
	private int? WideFoldBefore => AlwaysFolded is var (first, last) && first <= last ? last + 1 : null;

	private int? NarrowFoldBefore => FoldsWhenNarrow ? NarrowFolded.Last + 1 : null;

	// The level a narrow box links back to: the current page's parent.
	private MokaBreadcrumbItem? BackLevel
	{
		get
		{
			if (!IsTrail || _items.Count < 2)
			{
				return null;
			}

			MokaBreadcrumbItem parent = _items[^2];
			return UrlValues.SafeHref(parent.Href) is not null && !string.IsNullOrWhiteSpace(parent.Text) ? parent : null;
		}
	}

	private string ResolvedCopyText => CopyText ?? string.Join("/", _items
		.Select(item => item.Segment ?? item.Text)
		.Where(segment => !string.IsNullOrEmpty(segment)));

	// CSS can't tell the page when text is cut, so this guesses how many characters fit in ItemMaxWidth: two to a rem
	// at 13 px, less the link's padding. A name longer than that gets a tooltip, which is harmless when it fits.
	private int ItemCapacity
	{
		get
		{
			string width = (ItemMaxWidth ?? DefaultItemMaxWidth).Trim();
			if (TryReadLength(width, "rem", out double rem) || TryReadLength(width, "em", out rem))
			{
				return Math.Max(0, (int)((rem - 1) * 2));
			}

			if (TryReadLength(width, "px", out double px))
			{
				return Math.Max(0, (int)((px / 16 - 1) * 2));
			}

			return TryReadLength(width, "ch", out double chars) ? Math.Max(0, (int)chars - 2) : 22;
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		_rebuilding = false;
	}

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		_disposing = true;
		return base.DisposeAsyncCore();
	}

	internal static string? ShowClass(MokaBreadcrumbShow show) => show switch
	{
		MokaBreadcrumbShow.Wide => "moka-breadcrumb__wide",
		MokaBreadcrumbShow.Narrow => "moka-breadcrumb__narrow",
		_ => null
	};

	internal void RegisterItem(MokaBreadcrumbItem item)
	{
		if (_disposing || _items.Contains(item))
		{
			return;
		}

		if (HasRendered && !_rebuilding)
		{
			// A level that arrives later can sit anywhere in the markup, and the list only knows the order levels
			// arrived in. Creating every level again makes them arrive in markup order.
			_items.Clear();
			_generation++;
			_rebuilding = true;
			StateHasChanged();
			return;
		}

		_items.Add(item);
		StateHasChanged();
	}

	internal void UnregisterItem(MokaBreadcrumbItem item)
	{
		if (!_disposing && _items.Remove(item))
		{
			StateHasChanged();
		}
	}

	internal void ItemChanged(MokaBreadcrumbItem item)
	{
		if (!_disposing && _items.Contains(item))
		{
			StateHasChanged();
		}
	}

	internal MokaBreadcrumbPlace PlaceOf(MokaBreadcrumbItem item)
	{
		int index = _items.IndexOf(item);
		if (index < 0)
		{
			return MokaBreadcrumbPlace.Unheld;
		}

		MokaBreadcrumbShow show = ShowOf(Shows(index, false), Shows(index, true));
		return new MokaBreadcrumbPlace(index, index == _items.Count - 1, show, SeparatorAfter(2 * index + 1, show));
	}

	internal RenderFragment? FoldBefore(MokaBreadcrumbItem item)
	{
		int index = _items.IndexOf(item);
		if (index < 0)
		{
			return null;
		}

		if (WideFoldBefore == index)
		{
			return FoldUnit(false);
		}

		return NarrowFoldBefore == index ? FoldUnit(true) : null;
	}

	// A path's well, or a trail kept on one line, can be any width, so its current level may always be cut.
	internal bool MayCut(string? text, bool current) =>
		text is not null && (IsTrail ? text.Length > ItemCapacity || (OneLine && current) : current);

	private static bool TryReadLength(string width, string unit, out double value)
	{
		value = 0;
		return width.EndsWith(unit, StringComparison.OrdinalIgnoreCase) &&
		       double.TryParse(width[..^unit.Length], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}

	private static MokaBreadcrumbShow ShowOf(bool wide, bool narrow) => (wide, narrow) switch
	{
		(true, true) => MokaBreadcrumbShow.Always,
		(true, false) => MokaBreadcrumbShow.Wide,
		(false, true) => MokaBreadcrumbShow.Narrow,
		_ => MokaBreadcrumbShow.None
	};

	private static bool ShowsIn(MokaBreadcrumbShow show, bool narrow) =>
		show == MokaBreadcrumbShow.Always || show == (narrow ? MokaBreadcrumbShow.Narrow : MokaBreadcrumbShow.Wide);

	private static string FoldClass(MokaBreadcrumbShow show) => new CssBuilder("moka-breadcrumb-item moka-breadcrumb__fold")
		.AddClass(ShowClass(show))
		.Build();

	private static string MoreClass(bool open) => new CssBuilder("moka-breadcrumb__more")
		.AddClass("moka-breadcrumb__more--open", open)
		.Build();

	private static string SeparatorClass(MokaBreadcrumbShow show) => new CssBuilder("moka-breadcrumb-item__separator")
		.AddClass(ShowClass(show))
		.Build();

	private static string? DepthStyle(int depth) => new StyleBuilder()
		.AddStyle("--_breadcrumb-depth", depth.ToString(CultureInfo.InvariantCulture))
		.Build();

	private bool Shows(int index, bool narrow)
	{
		if (index == _items.Count - 1 && !ShowCurrent)
		{
			return false;
		}

		(int first, int last) = narrow ? NarrowFolded : AlwaysFolded;
		return index < first || index > last;
	}

	// Positions in markup order: a fold button in front of level k is 2k, level k itself 2k + 1.
	private int LastShownSlot(bool narrow)
	{
		int last = -1;
		for (int i = _items.Count - 1; i >= 0; i--)
		{
			if (Shows(i, narrow))
			{
				last = (2 * i) + 1;
				break;
			}
		}

		if (WideFoldBefore is int wide && (!narrow || !FoldsWhenNarrow))
		{
			last = Math.Max(last, 2 * wide);
		}

		if (narrow && NarrowFoldBefore is int folded)
		{
			last = Math.Max(last, 2 * folded);
		}

		return last;
	}

	private MokaBreadcrumbShow SeparatorAfter(int slot, MokaBreadcrumbShow show) => ShowOf(
		ShowsIn(show, false) && slot < LastShownSlot(false),
		ShowsIn(show, true) && slot < LastShownSlot(true));

	private string MoreLabelFor(int count)
	{
		string label = MoreLabel ?? DefaultMoreLabel;
		return count == 1 && string.Equals(label, DefaultMoreLabel, StringComparison.Ordinal)
			? "Show 1 more level"
			: label.Replace("{n}", count.ToString(CultureInfo.CurrentCulture), StringComparison.Ordinal);
	}

	private List<MokaBreadcrumbFoldedLevel> FoldedLevels((int First, int Last) range)
	{
		List<MokaBreadcrumbFoldedLevel> levels = [];
		for (int i = range.First; i <= range.Last; i++)
		{
			MokaBreadcrumbItem level = _items[i];
			levels.Add(new MokaBreadcrumbFoldedLevel(
				level.Text ?? level.Segment,
				UrlValues.SafeHref(level.Href),
				level.Icon ?? MokaIcons.File.Folder,
				i - range.First));
		}

		return levels;
	}

	private void SetFoldOpen(bool narrow, bool open)
	{
		if (narrow)
		{
			_narrowOpen = open;
		}
		else
		{
			_wideOpen = open;
		}
	}

	private void GoTo(string? href)
	{
		if (href is not null)
		{
			Navigation.NavigateTo(href);
		}
	}
}
