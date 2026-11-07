using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SettingsForm;

/// <summary>
///     The page around settings forms: a search box and a side list of sections beside the section showing. On a phone
///     the list is its own page, shown while nothing is <see cref="Active" />, and a section links back to it.
/// </summary>
public partial class MokaSettingsLayout : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Forms/SettingsForm/MokaSettingsLayout.razor.js";
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string HoverModule = "./_content/Moka.Red.Core/moka-hover.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	// Left to the browser the arrows move the cursor, and Enter submits a form around the layout.
	private static readonly Dictionary<string, object?>[] SearchKeys =
	[
		new()
		{
			["selector"] = "input",
			["keys"] = new[] { "ArrowDown", "ArrowUp", "Enter" },
			["unlessModified"] = true
		}
	];

	private readonly string _idPrefix = $"moka-settings-{Guid.NewGuid():N}";

	private string? _active;
	private int _activeResult;
	private bool _dirty;
	private DotNetObjectReference<MokaSettingsLayout>? _dotNetRef;
	private bool _focused;
	private bool _keysBound;
	private string? _lastActiveParameter;
	private string _query = string.Empty;
	private IReadOnlyList<MokaSettingsEntry> _results = [];
	private ElementReference _resultsRef;
	private bool _resultsShown;
	private ElementReference _root;
	private ElementReference _searchRef;
	private IJSObjectReference? _slashKey;
	private bool _trackingHover;

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	/// <summary>The rows of the side list.</summary>
	[Parameter]
	public IReadOnlyList<MokaSettingsSection> Sections { get; set; } = [];

	/// <summary>The <see cref="MokaSettingsSection.Id" /> of the section showing. On a phone, null shows the list instead.</summary>
	[Parameter]
	public string? Active { get; set; }

	/// <summary>Raised when a row without an address is picked, or the way back to the list is, with null.</summary>
	[Parameter]
	public EventCallback<string?> ActiveChanged { get; set; }

	/// <summary>Every setting that can be searched for. Without any, there's no search box.</summary>
	[Parameter]
	public IReadOnlyList<MokaSettingsEntry>? Entries { get; set; }

	/// <summary>Raised when a search result is picked, just before going to it.</summary>
	[Parameter]
	public EventCallback<MokaSettingsEntry> OnPick { get; set; }

	/// <summary>The most search results to show.</summary>
	[Parameter]
	public int MaxResults { get; set; } = 8;

	/// <summary>The section showing, and any forms in it.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The search box's placeholder and name.</summary>
	[Parameter]
	public string SearchLabel { get; set; } = "Search settings";

	/// <summary>What the results say when nothing matches.</summary>
	[Parameter]
	public string NoResultsText { get; set; } = "No setting matches that.";

	/// <summary>The side list's name for screen readers.</summary>
	[Parameter]
	public string ListLabel { get; set; } = "Settings sections";

	/// <summary>The address of the list on a phone. Left out, the way back is a button that clears <see cref="Active" />.</summary>
	[Parameter]
	public string? BackHref { get; set; }

	/// <summary>The text of the way back to the list on a phone.</summary>
	[Parameter]
	public string BackText { get; set; } = "All settings";

	/// <summary>Said for screen readers on the row of a section with unsaved edits.</summary>
	[Parameter]
	public string UnsavedLabel { get; set; } = "Unsaved edits";

	/// <inheritdoc />
	protected override string RootClass => "moka-settings-layout";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-settings-layout--list", _active is null)
		.AddClass(Class)
		.Build();

	private bool ResultsOpen => _focused && _query.Trim().Length > 0;

	private string ResultsId => $"{_idPrefix}-results";

	private string? ActiveResultId => ResultsOpen && _results.Count > 0 ? ResultId(_activeResult) : null;

	/// <summary>Called from JS just before a key press, with the result the mouse is on, so the key carries on from there.</summary>
	/// <param name="index">The result's position in the list.</param>
	[JSInvokable]
	public void OnItemHovered(int index) => _activeResult = index;

	internal void ReportDirty(bool dirty)
	{
		if (_dirty != dirty)
		{
			_dirty = dirty;
			RunDetached(StateHasChanged);
		}
	}

	// Every word typed must appear in the label, the keywords or the section. A label that starts with the first word
	// comes first, then one that holds it.
	internal static IReadOnlyList<MokaSettingsEntry> Search(IEnumerable<MokaSettingsEntry> entries, string query, int limit)
	{
		string[] words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (words.Length == 0)
		{
			return [];
		}

		return
		[
			.. entries
				.Where(entry => words.All(word =>
					$"{entry.Label} {entry.Keywords} {entry.Section}".Contains(word, StringComparison.OrdinalIgnoreCase)))
				.OrderBy(entry =>
				{
					if (entry.Label.StartsWith(words[0], StringComparison.OrdinalIgnoreCase))
					{
						return 0;
					}

					return entry.Label.Contains(words[0], StringComparison.OrdinalIgnoreCase) ? 1 : 2;
				})
				.Take(limit)
		];
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (Active != _lastActiveParameter)
		{
			_lastActiveParameter = Active;
			_active = Active;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		// The results go to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		bool shown = ResultsOpen && Entries is { Count: > 0 };
		if (_resultsShown != shown)
		{
			_resultsShown = shown;
			if (shown)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _idPrefix, _resultsRef, _searchRef, null,
					new Dictionary<string, object?> { ["placement"] = "bottom-start", ["width"] = "match" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _idPrefix);
			}
		}

		if (Entries is not { Count: > 0 })
		{
			return;
		}

		if (!_keysBound)
		{
			_keysBound = true;
			await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _searchRef, SearchKeys);
			_slashKey = await SafeModuleInvokeAsync<IJSObjectReference>(ModulePath, "bindSlashKey", _root);
		}

		if (!ResultsOpen)
		{
			_trackingHover = false;
		}
		else if (!_trackingHover)
		{
			_trackingHover = true;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(HoverModule, "trackHover", _resultsRef, _dotNetRef);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		await _slashKey.SafeInvokeVoidAsync("dispose");
		await _slashKey.SafeDisposeAsync();
		_dotNetRef?.Dispose();
		if (_resultsShown)
		{
			_resultsShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _idPrefix);
		}

		await base.DisposeAsyncCore();
	}

	private string ResultId(int index) => $"{_idPrefix}-result-{index.ToString(CultureInfo.InvariantCulture)}";

	private string ResultClass(int index) => new CssBuilder("moka-settings-layout__result")
		.AddClass("moka-settings-layout__result--active", index == _activeResult)
		.Build();

	private string RowClass(MokaSettingsSection section) => new CssBuilder("moka-settings-layout__row")
		.AddClass("moka-settings-layout__row--active", section.Id == _active)
		.Build();

	private void HandleQueryChanged(string query)
	{
		_query = query;
		_results = Search(Entries ?? [], query, MaxResults);
		_activeResult = 0;
	}

	private void HandleSearchFocus() => _focused = true;

	private void HandleSearchBlur() => _focused = false;

	private async Task HandleSearchKeyAsync(KeyboardEventArgs e)
	{
		if (e.AltKey || e.CtrlKey || e.MetaKey)
		{
			return;
		}

		switch (e.Key)
		{
			case "ArrowDown" when _results.Count > 0:
				_activeResult = (_activeResult + 1) % _results.Count;
				break;
			case "ArrowUp" when _results.Count > 0:
				_activeResult = (_activeResult + _results.Count - 1) % _results.Count;
				break;
			case "Enter" when _results.Count > 0:
				await PickAsync(_results[Math.Clamp(_activeResult, 0, _results.Count - 1)]);
				break;
			case "Escape":
				HandleQueryChanged(string.Empty);
				break;
		}
	}

	private async Task PickAsync(MokaSettingsEntry entry)
	{
		HandleQueryChanged(string.Empty);
		await OnPick.InvokeAsync(entry);

		// The fragment alone would resolve against the document's base address, not the page.
		string page = Navigation.ToBaseRelativePath(Navigation.Uri).Split('#')[0];
		Navigation.NavigateTo(entry.Route is null ? $"{page}{entry.Href}" : entry.Href);
	}

	private async Task PickSectionAsync(string? id)
	{
		_active = id;
		await ActiveChanged.InvokeAsync(id);
	}
}
