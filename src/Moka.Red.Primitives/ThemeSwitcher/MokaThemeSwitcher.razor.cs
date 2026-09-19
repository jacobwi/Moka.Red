using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.ThemeSwitcher;

/// <summary>
///     Dropdown component that lets users pick from multiple saved themes.
///     Displays a button with the current theme name and color swatches,
///     and opens a list of available themes on click. The list is a WAI-ARIA menu of
///     <c>menuitemradio</c> items: the arrow keys move through it, and Enter or Space pick.
/// </summary>
public partial class MokaThemeSwitcher : MokaVisualComponentBase
{
	private const string SwitcherModule = "./_content/Moka.Red.Primitives/ThemeSwitcher/MokaThemeSwitcher.razor.js";

	private readonly string _id = $"moka-theme-switcher-{Guid.NewGuid():N}";
	private ElementReference _root;
	private bool _isOpen;
	private bool _wasOpen;

	// The switcher shows _selectedTheme, not SelectedTheme: a pick changes it before the parent
	// answers, and a parent that does not bind SelectedTheme must not undo it by re-rendering.
	private MokaTheme? _selectedTheme;
	private MokaTheme? _lastSelectedThemeParameter;
	private bool _selectionSeeded;

	/// <summary>Available themes to choose from.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaThemeSwitcherItem> Themes { get; set; } = [];

	/// <summary>Currently selected theme. Two-way bindable.</summary>
	[Parameter]
	public MokaTheme? SelectedTheme { get; set; }

	/// <summary>Callback fired when the selected theme changes.</summary>
	[Parameter]
	public EventCallback<MokaTheme?> SelectedThemeChanged { get; set; }

	/// <summary>Whether to show color swatch previews in the dropdown items.</summary>
	[Parameter]
	public bool ShowPreview { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-theme-switcher";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-theme-switcher--open", _isOpen)
		.AddClass($"moka-theme-switcher--{MokaEnumHelpers.ToCssClass(Size)}")
		.AddClass(Class)
		.Build();

	// The root only anchors the menu and draws nothing, so the padding and radius go to the trigger,
	// the box the switcher shows. The margin stays on the root.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? TriggerStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string TriggerId => $"{_id}-trigger";

	private string MenuId => $"{_id}-menu";

	private string CurrentThemeName
	{
		get
		{
			if (_selectedTheme is null)
			{
				return "Select theme";
			}

			MokaThemeSwitcherItem? match = Themes.FirstOrDefault(IsSelected);
			return match?.Name ?? "Custom";
		}
	}

	// The visible text is only the theme's name, which says nothing about what the button does.
	private string TriggerLabel => _selectedTheme is null ? "Select theme" : $"Theme: {CurrentThemeName}";

	/// <summary>Has internal open/close state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// MokaTheme is a record, so an equal theme passed again (MokaTheme.Dark is a new instance
		// on every read) does not count as a new value.
		if (!_selectionSeeded || _lastSelectedThemeParameter != SelectedTheme)
		{
			_selectionSeeded = true;
			_lastSelectedThemeParameter = SelectedTheme;
			_selectedTheme = SelectedTheme;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The menu keyboard lives in the browser: it moves DOM focus between the items and closes
		// the menu when focus leaves the switcher.
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(SwitcherModule, "bindSwitcher", _root);
		}

		bool opened = _isOpen && !_wasOpen;
		_wasOpen = _isOpen;

		if (opened)
		{
			await SafeModuleInvokeVoidAsync(SwitcherModule, "focusCheckedItem", _root);
		}
	}

	private bool IsSelected(MokaThemeSwitcherItem item) => item.Theme == _selectedTheme;

	private string ItemCssClass(MokaThemeSwitcherItem item) => new CssBuilder("moka-theme-switcher__item")
		.AddClass("moka-theme-switcher__item--selected", IsSelected(item))
		.Build();

	private void ToggleDropdown() => _isOpen = !_isOpen;

	private async Task SelectTheme(MokaThemeSwitcherItem item)
	{
		_isOpen = false;

		if (item.Theme == _selectedTheme)
		{
			return;
		}

		_selectedTheme = item.Theme;
		await SelectedThemeChanged.InvokeAsync(item.Theme);
	}

	// Theme colors can come from outside the app, and each one goes into a declaration.
	private static string? SwatchStyle(MokaTheme theme, int index)
	{
		string color = GetSwatchColor(theme, index);
		return new StyleBuilder()
			.AddStyle("background-color", CssValues.IsColor(color) ? color.Trim() : null)
			.Build();
	}

	private static string GetSwatchColor(MokaTheme theme, int index) => index switch
	{
		0 => theme.Palette.Primary,
		1 => theme.Palette.Secondary,
		2 => theme.Palette.Success,
		3 => theme.Palette.Error,
		_ => theme.Palette.Primary
	};
}
