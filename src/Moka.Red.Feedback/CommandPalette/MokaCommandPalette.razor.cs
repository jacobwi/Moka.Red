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

	[Inject] private IMokaCommandPaletteService Service { get; set; } = default!;

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
		if (firstRender)
		{
			_dotNetRef = DotNetObjectReference.Create(this);
			_jsModule = await GetJsModuleAsync("./_content/Moka.Red.Feedback/moka-command-palette.js");
			await _jsModule.InvokeVoidAsync("registerShortcut", _dotNetRef);
		}

		if (_isOpen)
		{
			if (_jsModule is not null)
			{
				await _jsModule.InvokeVoidAsync("focusInput", _inputRef);
			}
		}
	}

	/// <summary>Invoked from JS when Ctrl+K is pressed.</summary>
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
				if (_focusedIndex >= 0 && _focusedIndex < _filteredCommands.Count)
				{
					_ = ExecuteCommand(_filteredCommands[_focusedIndex]);
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
	}

	private void SetFocusedIndex(int index) => _focusedIndex = index;

	private string ItemCss(MokaCommand command, int index) => new CssBuilder("moka-command-palette__item")
		.AddClass("moka-command-palette__item--focused", index == _focusedIndex)
		.AddClass("moka-command-palette__item--disabled", command.Disabled)
		.Build();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Service.OnToggle -= HandleToggle;

		if (_jsModule is not null)
		{
			try
			{
				await _jsModule.InvokeVoidAsync("dispose");
			}
			catch (JSDisconnectedException)
			{
				// Circuit already disconnected
			}
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
