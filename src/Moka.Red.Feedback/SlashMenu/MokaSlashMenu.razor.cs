using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.SlashMenu;

/// <summary>
///     An editor-anchored "/" insertion palette with delegated keyboard handling.
///     The menu renders with <c>position: absolute</c>: place it inside a
///     <c>position: relative</c> wrapper around the host editor and position it via the
///     <see cref="MokaComponentBase.Class" /> or <see cref="MokaComponentBase.Style" /> parameters.
///     The host input keeps focus and forwards keystrokes through <see cref="HandleKeyAsync" />;
///     a <c>true</c> return means the menu consumed the key and the host should skip its own
///     handling (preventDefault). Typical wiring:
///     <c>@onkeydown='async e =&gt; { if (menu is not null &amp;&amp; await menu.HandleKeyAsync(e)) return; ... }'</c>
///     with <c>@ref="menu"</c>. Focus stays in the host, so the host names the highlighted row
///     for screen readers: <c>aria-controls</c> set to <see cref="ListboxId" /> and
///     <c>aria-activedescendant</c> set to <see cref="ActiveOptionId" />.
///     On the new design (<see cref="MokaLook.Gate" />) items with a <see cref="MokaSlashMenuItem.Category" />
///     are listed in groups under it, the highlight counts rows in that order, and a footer says what the keys
///     do. <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" /> keep the original look: rows in
///     <see cref="Items" /> order with the category as a tag, and the key hints in the header. On both, the
///     highlighted row scrolls into view, and hovering a row moves the highlight in the browser without a
///     round trip.
/// </summary>
public partial class MokaSlashMenu : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/SlashMenu/MokaSlashMenu.razor.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	private readonly string _popupKey = $"moka-slash-menu-{Guid.NewGuid():N}";
	private readonly string _listboxId = $"moka-slash-menu-{Guid.NewGuid():N}-listbox";
	private ElementReference _root;
	private string? _shownPopup;
	private int _activeIndex;
	private bool _wasOpen;
	private bool? _lastOpen;
	private bool _open;
	private string? _lastQuery;
	private List<MokaSlashMenuItem> _filtered = [];
	private List<MokaSlashMenuSection> _sections = [];
	private ElementReference _listRef;
	private DotNetObjectReference<MokaSlashMenu>? _dotNetRef;
	private bool _listTracked;
	private bool _revealPending;

	// The new design groups the rows and draws them differently, so the markup is computed from the theme.
	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	/// <summary>Whether the menu is visible. Renders nothing when false. Two-way bindable.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes (Escape or a selection closes the menu).</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>
	///     The full list of insertable items. On the new design, items that share a
	///     <see cref="MokaSlashMenuItem.Category" /> are listed together under it, groups in the order their first
	///     item appears, after the items that have none. The original look lists them in this order.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaSlashMenuItem>? Items { get; set; }

	/// <summary>
	///     Filter text typed after the slash, managed by the parent.
	///     Case-insensitive contains match on Title, Keywords, and Category.
	///     The active index resets when the query changes.
	/// </summary>
	[Parameter]
	public string? Query { get; set; }

	/// <summary>Raised when an item is chosen via Enter or click. The menu closes afterwards.</summary>
	[Parameter]
	public EventCallback<MokaSlashMenuItem> OnSelect { get; set; }

	/// <summary>
	///     Maximum number of filtered items shown, counted in the order they are listed (on the new design,
	///     groups included). Defaults to 8. Values below 1 disable the cap.
	/// </summary>
	[Parameter]
	public int MaxVisible { get; set; } = 8;

	/// <summary>Uppercase header label. Null or empty hides the header row. Defaults to "Insert".</summary>
	[Parameter]
	public string? Header { get; set; } = "Insert";

	/// <summary>
	///     Whether the menu says what the keys do. On the new design a footer shows key caps with a word each
	///     (move, insert, close), with or without a <see cref="Header" />. The original look shows the caps in the
	///     header, so only with a header. Defaults to true.
	/// </summary>
	[Parameter]
	public bool ShowHints { get; set; } = true;

	/// <summary>
	///     The first line of the new design's empty state, shown when something typed matches nothing.
	///     <c>{query}</c> is replaced with what was typed. Null or empty hides the line. Defaults to
	///     <c>Nothing called "{query}"</c>. The original look says "No matches".
	/// </summary>
	[Parameter]
	public string? EmptyText { get; set; } = "Nothing called \"{query}\"";

	/// <summary>
	///     The second line of the new design's empty state, saying what Enter and Escape do now. While nothing matches, the menu
	///     hands Enter back to the editor. Null or empty hides the line. Defaults to
	///     "Enter adds a new line. Esc closes this and leaves the slash as text."
	/// </summary>
	[Parameter]
	public string? EmptyHint { get; set; } = "Enter adds a new line. Esc closes this and leaves the slash as text.";

	/// <summary>
	///     The element the menu opens against, usually the textarea. With it the menu opens in the browser's top layer,
	///     placed by <see cref="Placement" />, so a card or panel that hides its overflow can't cut it off and nothing
	///     later on the page covers it; leave placement styles out of <c>Style</c> then. Without it the menu is
	///     <c>position: absolute</c> and the app places it.
	/// </summary>
	[Parameter]
	public ElementReference? Anchor { get; set; }

	/// <summary>Where the menu opens against <see cref="Anchor" />, flipping when there's no room. Defaults to below its start.</summary>
	[Parameter]
	public MokaPopoverPosition Placement { get; set; } = MokaPopoverPosition.BottomStart;

	/// <summary>
	///     The id of the menu's listbox, for the host editor's <c>aria-controls</c> while the menu is
	///     open. It stays the same for the life of the component.
	/// </summary>
	public string ListboxId => _listboxId;

	/// <summary>
	///     The id of the highlighted option, for the host editor's <c>aria-activedescendant</c>, or
	///     null while the menu is closed or has no matches. Options are numbered by their place in
	///     the list as it is shown (on the new design, groups included). The menu works this out when it renders, so after the
	///     host changes <see cref="Open" /> or <see cref="Query" />, read it once the menu has rendered, in the
	///     host's <c>OnAfterRender</c>.
	/// </summary>
	public string? ActiveOptionId => _open && _filtered.Count > 0 ? OptionId(_activeIndex) : null;

	/// <inheritdoc />
	protected override string RootClass => "moka-slash-menu";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private bool IsNewDesign => CascadedTheme?.Look == MokaLook.Gate;

	// A field passed before the host's first render holds an empty reference, which can't be measured.
	private bool Anchored => Anchor is { Id.Length: > 0 };

	// What moka-popup.js was last told: a render that changes none of it sends nothing.
	private string? PopupState => _open && Anchored ? Placement.ToString() : null;

	private string? EmptyTitle => string.IsNullOrEmpty(EmptyText) || string.IsNullOrEmpty(_lastQuery)
		? null
		: EmptyText.Replace("{query}", _lastQuery, StringComparison.Ordinal);

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Open only seeds the state when the parent passes a new value. Copying it on every parent
		// render reopened an unbound menu the user had just closed with Escape.
		if (_lastOpen != Open)
		{
			_lastOpen = Open;
			_open = Open;
		}

		if (_open && !_wasOpen)
		{
			_activeIndex = 0;
		}

		_wasOpen = _open;

		var query = Query?.Trim();

		if (!string.Equals(query, _lastQuery, StringComparison.Ordinal))
		{
			_lastQuery = query;
			_activeIndex = 0;
			_revealPending = true;
		}

		FilterItems(query);
		_activeIndex = Math.Clamp(_activeIndex, 0, Math.Max(0, _filtered.Count - 1));
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		string? popup = PopupState;
		if (popup != _shownPopup)
		{
			_shownPopup = popup;
			if (popup is null)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _popupKey);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _popupKey, _root, Anchor!.Value, null,
					new Dictionary<string, object?> { ["placement"] = MokaEnumHelpers.ToCssClass(Placement) });
			}
		}

		if (!_open)
		{
			_listTracked = false;
			return;
		}

		// The list is a new element each time the menu opens, and it starts scrolled to the top.
		if (!_listTracked)
		{
			_listTracked = true;
			_revealPending = _activeIndex > 0;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(ModulePath, "trackHover", _listRef, _dotNetRef);
		}

		if (_revealPending)
		{
			_revealPending = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "reveal", _listRef, _activeIndex);
		}
	}

	/// <summary>
	///     Called from JS just before a key press, with the row the mouse is on, so the key carries on from there
	///     instead of from the row the keyboard last lit.
	/// </summary>
	/// <param name="index">The row's <c>data-moka-index</c>.</param>
	[JSInvokable]
	public void OnItemHovered(int index)
	{
		if (!_open || index < 0 || index >= _filtered.Count || index == _activeIndex)
		{
			return;
		}

		_activeIndex = index;

		// The key may be one the menu leaves to the editor, and then nothing else redraws it. The lit row has to
		// stay the one Enter inserts.
		StateHasChanged();
	}

	/// <summary>
	///     Delegated key handler for the host editor. Call this from the host input's
	///     <c>@onkeydown</c> handler before any other processing.
	///     ArrowDown/ArrowUp move the highlight with wrapping, Enter selects the active item
	///     (raises <see cref="OnSelect" /> and closes), Escape closes.
	/// </summary>
	/// <param name="e">The keyboard event forwarded by the host input.</param>
	/// <returns>
	///     True when the key was consumed by the menu, so the host can stop its own handling
	///     (preventDefault); false when the key should fall through to the editor.
	/// </returns>
	public async Task<bool> HandleKeyAsync(KeyboardEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(e);

		if (!_open)
		{
			return false;
		}

		switch (e.Key)
		{
			case "ArrowDown":
				MoveActive(1);
				return true;

			case "ArrowUp":
				MoveActive(-1);
				return true;

			case "Enter":
				if (_filtered.Count == 0)
				{
					return false;
				}

				await SelectAsync(_filtered[_activeIndex]);
				return true;

			case "Escape":
				await CloseAsync();
				return true;

			default:
				return false;
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_shownPopup is not null)
		{
			_shownPopup = null;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _popupKey);
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;
		await base.DisposeAsyncCore();
	}

	private void MoveActive(int delta)
	{
		if (_filtered.Count == 0)
		{
			return;
		}

		_activeIndex = (_activeIndex + delta + _filtered.Count) % _filtered.Count;
		_revealPending = true;
		StateHasChanged();
	}

	private async Task SelectAsync(MokaSlashMenuItem item)
	{
		if (OnSelect.HasDelegate)
		{
			await OnSelect.InvokeAsync(item);
		}

		await CloseAsync();
	}

	private async Task CloseAsync()
	{
		_open = false;

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}

		StateHasChanged();
	}

	private void FilterItems(string? query)
	{
		if (Items is null || Items.Count == 0)
		{
			_filtered = [];
			_sections = [];
			return;
		}

		IEnumerable<MokaSlashMenuItem> matches = string.IsNullOrEmpty(query)
			? Items
			: Items.Where(item => Matches(item, query));

		List<MokaSlashMenuItem> shown = IsNewDesign ? InGroups(matches) : [.. matches];

		if (MaxVisible > 0 && shown.Count > MaxVisible)
		{
			shown.RemoveRange(MaxVisible, shown.Count - MaxVisible);
		}

		_filtered = shown;
		_sections = Sections(shown);
	}

	// Rows are numbered in the order they are shown, so the arrows, Enter and the option ids all agree with
	// what is on screen.
	private static List<MokaSlashMenuItem> InGroups(IEnumerable<MokaSlashMenuItem> items)
	{
		List<MokaSlashMenuItem> ordered = [];
		List<List<MokaSlashMenuItem>> groups = [];
		Dictionary<string, List<MokaSlashMenuItem>> byCategory = new(StringComparer.Ordinal);

		foreach (MokaSlashMenuItem item in items)
		{
			string? category = Category(item);
			if (category is null)
			{
				ordered.Add(item);
				continue;
			}

			if (!byCategory.TryGetValue(category, out List<MokaSlashMenuItem>? group))
			{
				group = [];
				byCategory.Add(category, group);
				groups.Add(group);
			}

			group.Add(item);
		}

		foreach (List<MokaSlashMenuItem> group in groups)
		{
			ordered.AddRange(group);
		}

		return ordered;
	}

	private static List<MokaSlashMenuSection> Sections(List<MokaSlashMenuItem> shown)
	{
		List<MokaSlashMenuSection> sections = [];
		int start = 0;
		for (int i = 1; i <= shown.Count; i++)
		{
			if (i == shown.Count || !string.Equals(Category(shown[i]), Category(shown[start]), StringComparison.Ordinal))
			{
				sections.Add(new MokaSlashMenuSection(Category(shown[start]), start, i - start));
				start = i;
			}
		}

		return sections;
	}

	private static string? Category(MokaSlashMenuItem item) =>
		string.IsNullOrWhiteSpace(item.Category) ? null : item.Category;

	private static bool Matches(MokaSlashMenuItem item, string query) =>
		item.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
		|| (item.Keywords?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
		|| (item.Category?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

	// "Ctrl+Alt+2" is three keys, but "+" alone, a bullet in Markdown, names no key to ShortcutKeys.
	private static IReadOnlyList<string> ShortcutCaps(string shortcut)
	{
		IReadOnlyList<string> keys = ShortcutKeys.Split(shortcut);
		return keys.Count > 0 ? keys : [shortcut.Trim()];
	}

	private string OptionId(int index) => $"{_listboxId}-option-{index}";

	private string GroupLabelId(int start) => $"{_listboxId}-group-{start}";

	private string RowCss(int index) => new CssBuilder("moka-slash-menu-row")
		.AddClass("moka-slash-menu-row--active", index == _activeIndex)
		.Build();

	private readonly record struct MokaSlashMenuSection(string? Name, int Start, int Count);
}
