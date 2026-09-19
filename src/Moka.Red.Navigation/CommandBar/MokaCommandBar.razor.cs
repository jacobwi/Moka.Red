using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.CommandBar;

/// <summary>
///     A persistent horizontal command bar with three zones: left (breadcrumb/navigation),
///     center (search), and right (actions). Inspired by VS Code's top bar layout.
///     Unlike <c>MokaCommandPalette</c>, this is a visible, always-present bar, not a popup overlay.
/// </summary>
public partial class MokaCommandBar : MokaComponentBase
{
	private string? _lastSearchValue;
	private string? _searchValue;

	/// <summary>Content rendered in the left zone (breadcrumb, back button, etc.).</summary>
	[Parameter]
	public RenderFragment? LeftContent { get; set; }

	/// <summary>
	///     Content rendered in the center zone. When provided, replaces the built-in search input.
	/// </summary>
	[Parameter]
	public RenderFragment? CenterContent { get; set; }

	/// <summary>Content rendered in the right zone (action buttons, avatar, etc.).</summary>
	[Parameter]
	public RenderFragment? RightContent { get; set; }

	/// <summary>Placeholder text for the built-in search input. Defaults to "Search...".</summary>
	[Parameter]
	public string SearchPlaceholder { get; set; } = "Search...";

	/// <summary>The current search value. Supports two-way binding via <see cref="SearchValueChanged" />.</summary>
	[Parameter]
	public string? SearchValue { get; set; }

	/// <summary>Callback invoked when <see cref="SearchValue" /> changes.</summary>
	[Parameter]
	public EventCallback<string?> SearchValueChanged { get; set; }

	/// <summary>Callback invoked when the user presses Enter in the search input.</summary>
	[Parameter]
	public EventCallback<string> OnSearch { get; set; }

	/// <summary>Whether to show the built-in search input in the center zone. Defaults to true.</summary>
	[Parameter]
	public bool ShowSearch { get; set; } = true;

	/// <summary>Whether to use dense (compact) height. Defaults to false.</summary>
	[Parameter]
	public bool Dense { get; set; }

	/// <summary>Whether to show a bottom border. Defaults to true.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>Whether to show an elevation shadow. Defaults to false.</summary>
	[Parameter]
	public bool Elevated { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-command-bar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-command-bar--dense", Dense)
		.AddClass("moka-command-bar--bordered", Bordered)
		.AddClass("moka-command-bar--elevated", Elevated)
		.AddClass(Class)
		.Build();

	// An empty center zone is only a spacer, so it must not claim width the start zone needs.
	private string CenterCssClass => new CssBuilder("moka-command-bar__center")
		.AddClass("moka-command-bar__center--filled", CenterContent is not null || ShowSearch)
		.Build();

	/// <summary>Stateful component: always re-render to reflect search input changes.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// SearchValue only seeds the box when the parent passes a new value. Copying it on every
		// parent render wiped what the user had typed into an unbound search box.
		if (!string.Equals(_lastSearchValue, SearchValue, StringComparison.Ordinal))
		{
			_lastSearchValue = SearchValue;
			_searchValue = SearchValue;
		}
	}

	private async Task OnSearchInput(ChangeEventArgs e)
	{
		_searchValue = e.Value?.ToString();
		await SearchValueChanged.InvokeAsync(_searchValue);
	}

	private async Task OnSearchKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Enter" && OnSearch.HasDelegate)
		{
			await OnSearch.InvokeAsync(_searchValue ?? string.Empty);
		}
	}
}
