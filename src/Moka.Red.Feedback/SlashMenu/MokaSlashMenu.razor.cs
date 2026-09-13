using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

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
///     with <c>@ref="menu"</c>.
///     Pure CSS/state by design, no JS interop. Limitation: the active row is not scrolled
///     into view automatically when keyboard navigation moves past the visible area.
/// </summary>
public partial class MokaSlashMenu : MokaVisualComponentBase
{
	private int _activeIndex;
	private bool _wasOpen;
	private string? _lastQuery;
	private IReadOnlyList<MokaSlashMenuItem> _filtered = [];

	/// <summary>Whether the menu is visible. Renders nothing when false. Two-way bindable.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes (Escape or a selection closes the menu).</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>The full list of insertable items.</summary>
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

	/// <summary>Maximum number of filtered items shown. Defaults to 8. Values below 1 disable the cap.</summary>
	[Parameter]
	public int MaxVisible { get; set; } = 8;

	/// <summary>Uppercase header label. Null or empty hides the header row. Defaults to "Insert".</summary>
	[Parameter]
	public string? Header { get; set; } = "Insert";

	/// <summary>Whether the header shows the keyboard legend chips (navigate, select, close). Defaults to true.</summary>
	[Parameter]
	public bool ShowHints { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-slash-menu";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (Open && !_wasOpen)
		{
			_activeIndex = 0;
		}

		_wasOpen = Open;

		var query = Query?.Trim();

		if (!string.Equals(query, _lastQuery, StringComparison.Ordinal))
		{
			_lastQuery = query;
			_activeIndex = 0;
		}

		FilterItems(query);
		_activeIndex = Math.Clamp(_activeIndex, 0, Math.Max(0, _filtered.Count - 1));
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

		if (!Open)
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

	private void MoveActive(int delta)
	{
		if (_filtered.Count == 0)
		{
			return;
		}

		_activeIndex = (_activeIndex + delta + _filtered.Count) % _filtered.Count;
		StateHasChanged();
	}

	private void SetActiveIndex(int index) => _activeIndex = index;

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
		Open = false;

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
			return;
		}

		IEnumerable<MokaSlashMenuItem> source = Items;

		if (!string.IsNullOrEmpty(query))
		{
			source = source.Where(item => Matches(item, query));
		}

		if (MaxVisible > 0)
		{
			source = source.Take(MaxVisible);
		}

		_filtered = [.. source];
	}

	private static bool Matches(MokaSlashMenuItem item, string query) =>
		item.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
		|| (item.Keywords?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
		|| (item.Category?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

	private string RowCss(int index) => new CssBuilder("moka-slash-menu-row")
		.AddClass("moka-slash-menu-row--active", index == _activeIndex)
		.Build();
}
