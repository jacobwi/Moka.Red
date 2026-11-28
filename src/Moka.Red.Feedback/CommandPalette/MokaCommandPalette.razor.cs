using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Hotkeys;
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
	private readonly string _hotkeyId = $"moka-command-palette-{Guid.NewGuid():N}";
	private readonly MokaOverlayFocusTrap _focusTrap;
	private readonly List<MokaCommand> _pages = [];

	private string? _announcement;
	private ElementReference _dialogRef;
	private bool _disposed;
	private DotNetObjectReference<MokaCommandPalette>? _dotNetRef;
	private int _focusedIndex = -1;
	private MokaHotkey? _hotkey;
	private IMokaHotkeyService? _hotkeys;
	private ElementReference _inputRef;
	private bool _isOpen;
	private IJSObjectReference? _jsModule;
	private string? _registeredShortcut;
	private ElementReference _resultsRef;
	private List<PaletteSection> _sections = [];
	private string _searchText = string.Empty;
	private int? _shortcutHandle;
	private List<MokaCommand> _shown = [];
	private bool _statusLive;
	private bool _syncAgain;
	private bool _syncing;
	private bool _wasOpen;

	/// <summary>Creates the palette.</summary>
	public MokaCommandPalette() => _focusTrap = new MokaOverlayFocusTrap(
		element => SafeModuleInvokeAsync<int>(MokaOverlayFocusTrap.Module, "trapFocus", element),
		handle => SafeModuleInvokeVoidAsync(MokaOverlayFocusTrap.Module, "releaseFocus", handle));

	[Inject] private IMokaCommandPaletteService Service { get; set; } = default!;

	[Inject] private NavigationManager Navigation { get; set; } = default!;

	[Inject] private IServiceProvider Services { get; set; } = default!;

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

	/// <summary>
	///     The rest of the words the palette writes: the footer hints, the Recent heading, what screen readers hear when a
	///     command's <see cref="MokaCommand.Children" /> open, and how a shortcut list names the palette's shortcut.
	/// </summary>
	[Parameter]
	public MokaCommandPaletteText Text { get; set; } = new();

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
	///     How what is typed is matched against the commands. <see cref="MokaCommandMatching.Contains" />, the default,
	///     finds the text anywhere in a command and keeps registration order; <see cref="MokaCommandMatching.Fuzzy" />
	///     finds the letters in order with gaps allowed and puts the best matches first. <see cref="Filter" /> wins over it.
	/// </summary>
	[Parameter]
	public MokaCommandMatching Matching { get; set; }

	/// <summary>
	///     The app's own matching, given a command and what was typed: null leaves the command out, and a higher number
	///     puts it higher. Equal scores keep registration order. Wins over <see cref="Matching" />. Not asked while the
	///     search box is empty, when every command shows.
	/// </summary>
	[Parameter]
	public Func<MokaCommand, string, int?>? Filter { get; set; }

	/// <summary>
	///     The order groups are listed in, by <see cref="MokaCommand.Group" /> name. Groups named here come first in this
	///     order, and the others after them by name, as they are without it. Commands without a group stay at the top.
	///     While results are ranked (<see cref="MokaCommandMatching.Fuzzy" /> or a <see cref="Filter" />, with something
	///     typed) groups follow their best result instead.
	/// </summary>
	[Parameter]
	public IReadOnlyList<string>? GroupOrder { get; set; }

	/// <summary>
	///     Whether the commands run most recently (<see cref="IMokaCommandPaletteService.Recent" />) are listed first,
	///     under the <see cref="MokaCommandPaletteText.Recent" /> heading, while the search box is empty at the top level.
	///     They leave their own groups while they show there. Off by default.
	/// </summary>
	[Parameter]
	public bool ShowRecent { get; set; }

	/// <summary>How many recent commands <see cref="ShowRecent" /> lists at most.</summary>
	[Parameter]
	public int RecentCount { get; set; } = 5;

	/// <summary>
	///     The keyboard shortcut that opens and closes the palette: modifiers, then one key, joined with
	///     <c>+</c>. The default <c>"Mod+K"</c> is Ctrl+K, or Cmd+K on a Mac; others look like
	///     <c>"Ctrl+Shift+P"</c> or <c>"Alt+Space"</c>. The modifiers are Mod, Ctrl, Cmd, Alt and Shift, and
	///     a press only counts when exactly those are held. Null or empty turns the shortcut off. While the app's
	///     hotkeys (<see cref="IMokaHotkeyService" />) have a <c>MokaHotkeyHost</c> listening, the shortcut is registered
	///     there, so it shows in the app's shortcut list under <see cref="MokaCommandPaletteText.ShortcutDescription" />;
	///     without one the palette listens for it itself.
	/// </summary>
	[Parameter]
	public string? Shortcut { get; set; } = "Mod+K";

	/// <inheritdoc />
	protected override string RootClass => "moka-command-palette";

	private MokaCommand? CurrentPage => _pages.Count > 0 ? _pages[^1] : null;

	private bool HasResults => _shown.Count > 0;

	private string? ActiveOptionId => _focusedIndex >= 0 && _focusedIndex < _shown.Count ? OptionId(_focusedIndex) : null;

	private string? StatusText => !_statusLive ? null : HasResults ? _announcement : NoResultsText;

	private string PageId => $"{_listboxId}-page";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		Service.OnToggle += HandleToggle;
		_hotkeys = Services.GetService<IMokaHotkeyService>();
		if (_hotkeys is not null)
		{
			_hotkeys.ListeningChanged += HandleListeningChanged;
		}

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
			}

			await SyncShortcutAsync();

			if (_isOpen && !_wasOpen)
			{
				await SafeModuleInvokeVoidAsync(HoverModule, "trackHover", _resultsRef, _dotNetRef);
				await SafeModuleInvokeVoidAsync(PaletteModule, "watchHighlight", _inputRef, _resultsRef);
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
			_pages.Clear();
			_announcement = null;
			_searchText = string.Empty;
			FilterCommands();
		}

		InvokeAsync(StateHasChanged);
	}

	private void HandleListeningChanged()
	{
		if (_dotNetRef is not null)
		{
			RunDetached(ResyncShortcutAsync);
		}
	}

	private async Task ResyncShortcutAsync()
	{
		try
		{
			await SyncShortcutAsync();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
	}

	private void HandleBackdropClick() => Service.Close();

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "Escape" when CurrentPage is not null:
				GoBack();
				break;
			case "Escape":
				Service.Close();
				break;
			case "Backspace" when CurrentPage is not null && _searchText.Length == 0:
				GoBack();
				break;
			case "ArrowDown":
				if (_shown.Count > 0)
				{
					_focusedIndex = (_focusedIndex + 1) % _shown.Count;
				}

				break;
			case "ArrowUp":
				if (_shown.Count > 0)
				{
					_focusedIndex = (_focusedIndex - 1 + _shown.Count) % _shown.Count;
				}

				break;
			case "Enter":
				if (_focusedIndex >= 0 && _focusedIndex < _shown.Count)
				{
					await ExecuteCommand(_shown[_focusedIndex], true);
				}

				break;
		}
	}

	private void OnSearchChanged()
	{
		_announcement = null;
		FilterCommands();
	}

	private void OpenPage(MokaCommand command)
	{
		_pages.Add(command);
		_searchText = string.Empty;
		FilterCommands();
		_announcement = Text.PageOpened(command.Title);
	}

	private void GoBack()
	{
		MokaCommand left = _pages[^1];
		_pages.RemoveAt(_pages.Count - 1);
		_searchText = string.Empty;
		FilterCommands();

		int back = _shown.IndexOf(left);
		if (back >= 0)
		{
			_focusedIndex = back;
		}

		_announcement = CurrentPage is { } page ? Text.PageOpened(page.Title) : Label;
	}

	private void FilterCommands()
	{
		IReadOnlyList<MokaCommand> source = CurrentPage?.Children ?? Service.Commands;
		List<MokaCommand> recent = [];
		List<MokaCommand> matched;
		bool ranked = false;

		if (string.IsNullOrWhiteSpace(_searchText))
		{
			if (ShowRecent && CurrentPage is null)
			{
				recent = RecentCommands();
			}

			matched = source
				.Where(c => !recent.Contains(c))
				.Take(Math.Max(0, MaxResults - recent.Count))
				.ToList();
		}
		else if (Filter is not null || Matching == MokaCommandMatching.Fuzzy)
		{
			ranked = true;
			string typed = _searchText;
			Func<MokaCommand, int?> score = Filter is { } filter
				? command => filter(command, typed)
				: FuzzyScore(MokaCommandSearch.FuzzyQuery(typed));

			matched = source
				.Select(command => (Command: command, Score: score(command)))
				.Where(result => result.Score is not null)
				.OrderByDescending(result => result.Score!.Value)
				.Take(MaxResults)
				.Select(result => result.Command)
				.ToList();
		}
		else
		{
			string search = _searchText;
			matched = source.Where(c => MokaCommandSearch.Contains(c, search)).Take(MaxResults).ToList();
		}

		_sections = BuildSections(recent, matched, ranked);
		_shown = _sections.SelectMany(section => section.Commands).ToList();
		_focusedIndex = _shown.Count > 0 ? 0 : -1;
	}

	private static Func<MokaCommand, int?> FuzzyScore(string query) => command => MokaCommandSearch.Fuzzy(command, query);

	private List<MokaCommand> RecentCommands()
	{
		List<MokaCommand> found = [];
		int limit = Math.Min(RecentCount, MaxResults);
		foreach (string id in Service.Recent)
		{
			if (found.Count >= limit)
			{
				break;
			}

			if (Find(Service.Commands, id, []) is { } command && !found.Contains(command))
			{
				found.Add(command);
			}
		}

		return found;
	}

	// A command may sit anywhere in the tree, and an app could hand one of its own ancestors back as a child, so the walk
	// remembers where it has been.
	private static MokaCommand? Find(IReadOnlyList<MokaCommand> commands, string id, HashSet<MokaCommand> seen)
	{
		foreach (MokaCommand command in commands)
		{
			if (!seen.Add(command))
			{
				continue;
			}

			if (string.Equals(command.Id, id, StringComparison.Ordinal))
			{
				return command;
			}

			if (command.Children is { } children && Find(children, id, seen) is { } child)
			{
				return child;
			}
		}

		return null;
	}

	// With ShowGroups the list shows commands grouped, and the highlight and Enter count in that order. Commands without a
	// group stay first, so their rows never sit under another group's heading.
	private List<PaletteSection> BuildSections(List<MokaCommand> recent, List<MokaCommand> matched, bool ranked)
	{
		List<PaletteSection> sections = [];
		if (recent.Count > 0)
		{
			sections.Add(new PaletteSection(ShowGroups ? Text.Recent : null, recent));
		}

		if (!ShowGroups)
		{
			if (matched.Count > 0)
			{
				sections.Add(new PaletteSection(null, matched));
			}

			return sections;
		}

		IEnumerable<IGrouping<string, MokaCommand>> groups = matched.GroupBy(c => c.Group ?? string.Empty);
		IEnumerable<IGrouping<string, MokaCommand>> ordered = ranked
			? groups.OrderBy(group => group.Key.Length == 0 ? 0 : 1)
			: GroupOrder is { Count: > 0 } order
				? groups.OrderBy(group => GroupRank(order, group.Key)).ThenBy(group => group.Key)
				: groups.OrderBy(group => group.Key);

		foreach (IGrouping<string, MokaCommand> group in ordered)
		{
			sections.Add(new PaletteSection(group.Key.Length == 0 ? null : group.Key, group.ToList()));
		}

		return sections;
	}

	private static int GroupRank(IReadOnlyList<string> order, string group)
	{
		if (group.Length == 0)
		{
			return -1;
		}

		for (int index = 0; index < order.Count; index++)
		{
			if (string.Equals(order[index], group, StringComparison.Ordinal))
			{
				return index;
			}
		}

		return order.Count;
	}

	// Navigating to a javascript: URL would run it, since NavigateTo hands a URL outside the app to the browser, and so
	// would a link to one.
	private static string? HrefOf(MokaCommand command) =>
		string.IsNullOrEmpty(command.Href) ? null : UrlValues.SafeHref(command.Href);

	private static bool OpensNewTab(MokaCommand command) =>
		command.Children is null && HrefOf(command) is not null && UrlValues.OpensNewTab(command.Target);

	// A click on a row that opens another tab was the link's own, so only Enter opens the tab here, before anything else
	// runs, while the browser still counts the key press.
	private async Task ExecuteCommand(MokaCommand command, bool byKeyboard)
	{
		if (command.Disabled)
		{
			return;
		}

		if (command.Children is not null)
		{
			OpenPage(command);
			return;
		}

		string? href = HrefOf(command);
		bool newTab = OpensNewTab(command);
		if (newTab && byKeyboard)
		{
			await SafeModuleInvokeVoidAsync(PaletteModule, "openTab", href, command.Target);
		}

		Service.SetRecent(Service.Recent.Prepend(command.Id));
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

	// The render and a host starting or stopping can both get here, and each awaits the browser, so a second call while
	// one runs only asks for another pass. Two at once would bind the browser listener and the registry's together, and
	// the shortcut would toggle twice.
	private async Task SyncShortcutAsync()
	{
		if (_syncing)
		{
			_syncAgain = true;
			return;
		}

		_syncing = true;
		try
		{
			do
			{
				_syncAgain = false;
				await SyncShortcutOnceAsync();
			} while (_syncAgain && !_disposed);
		}
		finally
		{
			_syncing = false;
		}
	}

	private async Task SyncShortcutOnceAsync()
	{
		if (_disposed)
		{
			return;
		}

		if (_hotkeys is { IsListening: true })
		{
			await ReleaseBrowserShortcutAsync();
			RegisterHotkey(_hotkeys);
			return;
		}

		UnregisterHotkey();
		if (_jsModule is null || _dotNetRef is null)
		{
			return;
		}

		// The handle scopes the keydown listener to this instance so disposing one palette does not unhook the shortcut
		// for any other.
		if (_shortcutHandle is null)
		{
			int handle = await _jsModule.InvokeAsync<int>("registerShortcut", _dotNetRef, Shortcut);
			_shortcutHandle = handle;
			_registeredShortcut = Shortcut;
			if (_disposed)
			{
				await ReleaseBrowserShortcutAsync();
			}
		}
		else if (_registeredShortcut != Shortcut)
		{
			await _jsModule.InvokeVoidAsync("updateShortcut", _shortcutHandle.Value, Shortcut);
			_registeredShortcut = Shortcut;
		}
	}

	private async Task ReleaseBrowserShortcutAsync()
	{
		if (_shortcutHandle is not { } handle)
		{
			return;
		}

		_shortcutHandle = null;
		_registeredShortcut = null;
		await _jsModule.SafeInvokeVoidAsync("dispose", handle);
	}

	// Matches what the palette's own listener does: it toggles over other dialogs, a key that types a character doesn't
	// fire in a field, and holding the keys toggles once.
	private void RegisterHotkey(IMokaHotkeyService hotkeys)
	{
		if (string.IsNullOrWhiteSpace(Shortcut))
		{
			UnregisterHotkey();
			return;
		}

		if (_hotkey is not null && _hotkey.Keys == Shortcut && _hotkey.Description == Text.ShortcutDescription)
		{
			return;
		}

		// Registering again under the same id replaces the hotkey in place. Disposing the earlier registration would
		// unregister the id, and with it the new one.
		_hotkey = new MokaHotkey
		{
			Id = _hotkeyId,
			Keys = Shortcut,
			Description = Text.ShortcutDescription,
			AllowInModal = true,
			OnInvokeSync = Service.Toggle
		};
		hotkeys.Register(_hotkey);
	}

	private void UnregisterHotkey()
	{
		if (_hotkey is null)
		{
			return;
		}

		_hotkey = null;
		_hotkeys?.Unregister(_hotkeyId);
	}

	private string OptionId(int index) => $"{_listboxId}-option-{index}";

	private string GroupHeadingId(int firstIndex) => $"{_listboxId}-group-{firstIndex}";

	private string ItemCss(MokaCommand command, int index) => new CssBuilder("moka-command-palette__item")
		.AddClass("moka-command-palette__item--focused", index == _focusedIndex)
		.AddClass("moka-command-palette__item--disabled", command.Disabled)
		.Build();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		Service.OnToggle -= HandleToggle;
		if (_hotkeys is not null)
		{
			_hotkeys.ListeningChanged -= HandleListeningChanged;
		}

		UnregisterHotkey();
		await _focusTrap.ReleaseAsync();
		await ReleaseBrowserShortcutAsync();
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private sealed record PaletteSection(string? Heading, List<MokaCommand> Commands);
}
