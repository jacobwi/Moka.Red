using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.CommandPalette;

/// <summary>
///     A Ctrl+K spotlight search overlay for discovering and executing commands.
///     Register commands via <see cref="IMokaCommandPaletteService" /> and place this
///     component once in your layout (e.g., after MokaDialogHost).
/// </summary>
public partial class MokaCommandPalette : MokaComponentBase
{
	private DotNetObjectReference<MokaCommandPalette>? _dotNetRef;
	private List<MokaCommand> _filteredCommands = [];
	private int _focusedIndex = -1;
	private ElementReference _inputRef;
	private bool _isOpen;
	private IJSObjectReference? _jsModule;

	private string _searchText = string.Empty;
	private int? _shortcutHandle;
	private string? _registeredShortcut;

	[Inject] private IMokaCommandPaletteService Service { get; set; } = default!;

	[Inject] private NavigationManager Navigation { get; set; } = default!;

	/// <summary>Placeholder text for the search input.</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Search commands...";

	/// <summary>Text shown when no commands match the search.</summary>
	[Parameter]
	public string NoResultsText { get; set; } = "No commands found";

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
		try
		{
			if (firstRender)
			{
				_dotNetRef = DotNetObjectReference.Create(this);
				_jsModule = await GetJsModuleAsync("./_content/Moka.Red.Feedback/moka-command-palette.js");

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

			if (_isOpen && _jsModule is not null)
			{
				await _jsModule.InvokeVoidAsync("focusInput", _inputRef);
			}
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected before the module could be wired up
		}
	}

	/// <summary>Invoked from JS when the <see cref="Shortcut" /> is pressed.</summary>
	[JSInvokable]
	public void ToggleFromJs() => Service.Toggle();

	private void HandleToggle()
	{
		_isOpen = Service.IsOpen;
		if (_isOpen)
		{
			_searchText = string.Empty;
			FilterCommands();
		}

		InvokeAsync(StateHasChanged);
	}

	private void HandleBackdropClick() => Service.Close();

	private void HandleKeyDown(KeyboardEventArgs e)
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
					_ = ExecuteCommand(shown[_focusedIndex]);
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

	private async Task ExecuteCommand(MokaCommand command)
	{
		if (command.Disabled)
		{
			return;
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

		if (!string.IsNullOrEmpty(command.Href))
		{
			Navigation.NavigateTo(command.Href);
		}
	}

	private void SetFocusedIndex(int index) => _focusedIndex = index;

	// With ShowGroups the list shows commands grouped and the groups sorted by name, and the
	// highlight counts in that order. Enter used to index the filtered list in registration order,
	// so it ran a different command than the highlighted one.
	private IEnumerable<IGrouping<string, MokaCommand>> GroupedCommands() =>
		_filteredCommands.GroupBy(c => c.Group ?? string.Empty).OrderBy(g => g.Key);

	private List<MokaCommand> DisplayedCommands() =>
		ShowGroups ? GroupedCommands().SelectMany(g => g).ToList() : _filteredCommands;

	private string ItemCss(MokaCommand command, int index) => new CssBuilder("moka-command-palette__item")
		.AddClass("moka-command-palette__item--focused", index == _focusedIndex)
		.AddClass("moka-command-palette__item--disabled", command.Disabled)
		.Build();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Service.OnToggle -= HandleToggle;

		if (_jsModule is not null && _shortcutHandle is not null)
		{
			try
			{
				await _jsModule.InvokeVoidAsync("dispose", _shortcutHandle.Value);
			}
			catch (JSDisconnectedException)
			{
				// Circuit already disconnected
			}
		}

		_shortcutHandle = null;
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
