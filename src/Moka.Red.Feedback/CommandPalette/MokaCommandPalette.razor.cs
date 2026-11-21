using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;

namespace Moka.Red.Feedback.CommandPalette;

/// <summary>
///     A Ctrl+K spotlight search overlay for discovering and executing commands.
///     Register commands via <see cref="IMokaCommandPaletteService" /> and place this
///     component once in your layout (e.g., after MokaDialogHost). To screen readers it is a modal dialog holding the
///     WAI-ARIA combobox pattern: the search box keeps focus and points at the highlighted command in a listbox through
///     <c>aria-activedescendant</c>.
/// </summary>
public partial class MokaCommandPalette : MokaComponentBase
{
	private const string HoverModule = "./_content/Moka.Red.Core/moka-hover.js";
	private const string PaletteModule = "./_content/Moka.Red.Feedback/moka-command-palette.js";

	private readonly string _listboxId = $"moka-command-palette-{Guid.NewGuid():N}-listbox";
	private readonly MokaOverlayFocusTrap _focusTrap;

	private ElementReference _dialogRef;
	private DotNetObjectReference<MokaCommandPalette>? _dotNetRef;
	private List<MokaCommand> _filteredCommands = [];
	private int _focusedIndex = -1;
	private ElementReference _inputRef;
	private bool _isOpen;
	private IJSObjectReference? _jsModule;
	private ElementReference _resultsRef;
	private bool _statusLive;
	private bool _wasOpen;

	private string _searchText = string.Empty;
	private int? _shortcutHandle;
	private string? _registeredShortcut;

	/// <summary>Creates the palette.</summary>
	public MokaCommandPalette() => _focusTrap = new MokaOverlayFocusTrap(
		element => SafeModuleInvokeAsync<int>(MokaOverlayFocusTrap.Module, "trapFocus", element),
		handle => SafeModuleInvokeVoidAsync(MokaOverlayFocusTrap.Module, "releaseFocus", handle));

	[Inject] private IMokaCommandPaletteService Service { get; set; } = default!;

	[Inject] private NavigationManager Navigation { get; set; } = default!;

	/// <summary>Placeholder text for the search input.</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Search commands...";

	/// <summary>
	///     The accessible name of the palette's dialog and of the list of commands in it, which screen readers say when
	///     the palette opens.
	/// </summary>
	[Parameter]
	public string Label { get; set; } = "Command palette";

	/// <summary>
	///     The accessible name of the search box, a combobox to screen readers. The placeholder is only a hint and goes
	///     away once something is typed, so the box carries a name of its own.
	/// </summary>
	[Parameter]
	public string SearchLabel { get; set; } = "Search commands";

	/// <summary>
	///     Text shown when no commands match the search. Screen readers hear it too, from a polite live region, when the
	///     list empties.
	/// </summary>
	[Parameter]
	public string NoResultsText { get; set; } = "No commands found";

	/// <summary>
	///     What screen readers hear after the title of a command whose <see cref="MokaCommand.Target" /> opens another tab,
	///     after a comma. The row shows an external-link mark.
	/// </summary>
	[Parameter]
	public string NewTabText { get; set; } = "opens in a new tab";

	/// <summary>Maximum number of results to display.</summary>
	[Parameter]
	public int MaxResults { get; set; } = 20;

	/// <summary>Whether to show keyboard shortcut hints on each command.</summary>
	[Parameter]
	public bool ShowShortcuts { get; set; } = true;

	/// <summary>Whether to group commands by their <see cref="MokaCommand.Group" /> property.</summary>
	[Parameter]
	public bool ShowGroups { get; set; } = true;

	/// <summary>
	///     The keyboard shortcut that opens and closes the palette: modifiers, then one key, joined with
	///     <c>+</c>. The default <c>"Mod+K"</c> is Ctrl+K, or Cmd+K on a Mac; others look like
	///     <c>"Ctrl+Shift+P"</c> or <c>"Alt+Space"</c>. The modifiers are Mod, Ctrl, Cmd, Alt and Shift, and
	///     a press only counts when exactly those are held. Null or empty turns the shortcut off.
	/// </summary>
	[Parameter]
	public string? Shortcut { get; set; } = "Mod+K";

	/// <inheritdoc />
	protected override string RootClass => "moka-command-palette";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		Service.OnToggle += HandleToggle;
		FilterCommands();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// A live region added with its text already in it isn't announced, so the status region opens empty and takes
		// the no-results text one render later.
		if (_isOpen && !_statusLive)
		{
			_statusLive = true;
			if (!HasResults)
			{
				StateHasChanged();
			}
		}

		try
		{
			if (firstRender)
			{
				_dotNetRef = DotNetObjectReference.Create(this);
				_jsModule = await GetJsModuleAsync(PaletteModule);

				// The handle scopes the keydown listener to this instance so disposing
				// one palette does not unhook the shortcut for any other.
				_shortcutHandle = await _jsModule.InvokeAsync<int>("registerShortcut", _dotNetRef, Shortcut);
				_registeredShortcut = Shortcut;
			}
			else if (_jsModule is not null && _shortcutHandle is not null && _registeredShortcut != Shortcut)
			{
				await _jsModule.InvokeVoidAsync("updateShortcut", _shortcutHandle.Value, Shortcut);
				_registeredShortcut = Shortcut;
			}

			if (_isOpen && !_wasOpen)
			{
				await SafeModuleInvokeVoidAsync(HoverModule, "trackHover", _resultsRef, _dotNetRef);
			}

			_wasOpen = _isOpen;

			if (_isOpen && _jsModule is not null)
			{
				await _jsModule.InvokeVoidAsync("focusInput", _inputRef);
			}

			// The palette is a modal dialog, so Tab stays inside it while it's open and focus goes back where it was.
			await _focusTrap.SyncAsync(() => _isOpen, _dialogRef);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			// Circuit disconnected before the module could be wired up
		}
	}

	/// <summary>Invoked from JS when the <see cref="Shortcut" /> is pressed.</summary>
	[JSInvokable]
	public void ToggleFromJs() => Service.Toggle();

	/// <summary>Called from JS just before a key press, with the command the mouse is on, so the key carries on from there.</summary>
	/// <param name="index">The command's position in display order.</param>
	[JSInvokable]
	public void OnItemHovered(int index) => _focusedIndex = index;

	private void HandleToggle()
	{
		_isOpen = Service.IsOpen;
		_statusLive = false;
		if (_isOpen)
		{
			_searchText = string.Empty;
			FilterCommands();
		}

		InvokeAsync(StateHasChanged);
	}

	private void HandleBackdropClick() => Service.Close();

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "Escape":
				Service.Close();
				break;
			case "ArrowDown":
				if (_filteredCommands.Count > 0)
				{
					_focusedIndex = (_focusedIndex + 1) % _filteredCommands.Count;
				}

				break;
			case "ArrowUp":
				if (_filteredCommands.Count > 0)
				{
					_focusedIndex = (_focusedIndex - 1 + _filteredCommands.Count) % _filteredCommands.Count;
				}

				break;
			case "Enter":
				List<MokaCommand> shown = DisplayedCommands();
				if (_focusedIndex >= 0 && _focusedIndex < shown.Count)
				{
					await ExecuteCommand(shown[_focusedIndex], true);
				}

				break;
		}
	}

	private void OnSearchChanged() => FilterCommands();

	private void FilterCommands()
	{
		if (string.IsNullOrWhiteSpace(_searchText))
		{
			_filteredCommands = Service.Commands.Take(MaxResults).ToList();
		}
		else
		{
			string search = _searchText;
			_filteredCommands = Service.Commands
				.Where(c =>
					c.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					(c.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(c.Keywords?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(c.Group?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
				.Take(MaxResults)
				.ToList();
		}

		_focusedIndex = _filteredCommands.Count > 0 ? 0 : -1;
	}

	// Navigating to a javascript: URL would run it, since NavigateTo hands a URL outside the app to the browser, and so
	// would a link to one.
	private static string? HrefOf(MokaCommand command) =>
		string.IsNullOrEmpty(command.Href) ? null : UrlValues.SafeHref(command.Href);

	private static bool OpensNewTab(MokaCommand command) =>
		HrefOf(command) is not null && UrlValues.OpensNewTab(command.Target);

	// A click on a row that opens another tab was the link's own, so only Enter opens the tab here, before anything else
	// runs, while the browser still counts the key press.
	private async Task ExecuteCommand(MokaCommand command, bool byKeyboard)
	{
		if (command.Disabled)
		{
			return;
		}

		string? href = HrefOf(command);
		bool newTab = OpensNewTab(command);
		if (newTab && byKeyboard)
		{
			await SafeModuleInvokeVoidAsync(PaletteModule, "openTab", href, command.Target);
		}

		Service.Close();

		if (command.OnExecute is not null)
		{
			await command.OnExecute();
		}
		else
		{
			command.OnExecuteSync?.Invoke();
		}

		if (href is not null && !newTab)
		{
			Navigation.NavigateTo(href);
		}
	}

	// With ShowGroups the list shows commands grouped and the groups sorted by name, and the
	// highlight counts in that order. Enter used to index the filtered list in registration order,
	// so it ran a different command than the highlighted one.
	private IEnumerable<IGrouping<string, MokaCommand>> GroupedCommands() =>
		_filteredCommands.GroupBy(c => c.Group ?? string.Empty).OrderBy(g => g.Key);

	private List<MokaCommand> DisplayedCommands() =>
		ShowGroups ? GroupedCommands().SelectMany(g => g).ToList() : _filteredCommands;

	private bool HasResults => _filteredCommands.Count > 0;

	private string? ActiveOptionId => _focusedIndex >= 0 && _focusedIndex < _filteredCommands.Count ? OptionId(_focusedIndex) : null;

	private string? StatusText => _statusLive && !HasResults ? NoResultsText : null;

	private string OptionId(int index) => $"{_listboxId}-option-{index}";

	private string GroupHeadingId(int firstIndex) => $"{_listboxId}-group-{firstIndex}";

	private string ItemCss(MokaCommand command, int index) => new CssBuilder("moka-command-palette__item")
		.AddClass("moka-command-palette__item--focused", index == _focusedIndex)
		.AddClass("moka-command-palette__item--disabled", command.Disabled)
		.Build();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Service.OnToggle -= HandleToggle;
		await _focusTrap.ReleaseAsync();

		if (_shortcutHandle is not null)
		{
			await _jsModule.SafeInvokeVoidAsync("dispose", _shortcutHandle.Value);
		}

		_shortcutHandle = null;
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
