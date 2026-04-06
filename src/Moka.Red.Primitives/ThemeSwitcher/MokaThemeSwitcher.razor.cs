using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.ThemeSwitcher;

/// <summary>
///     Dropdown component that lets users pick from multiple saved themes.
///     Displays a button with the current theme name and color swatches,
///     and opens a list of available themes on click.
/// </summary>
public partial class MokaThemeSwitcher : MokaVisualComponentBase
{
	private bool _isOpen;

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

	private string CurrentThemeName
	{
		get
		{
			if (SelectedTheme is null)
			{
				return "Select theme";
			}

			MokaThemeSwitcherItem? match = Themes.FirstOrDefault(t => t.Theme == SelectedTheme);
			return match?.Name ?? "Custom";
		}
	}

	/// <summary>Has internal open/close state.</summary>
	protected override bool ShouldRender() => true;

	private void ToggleDropdown() => _isOpen = !_isOpen;

	private async Task SelectTheme(MokaThemeSwitcherItem item)
	{
		SelectedTheme = item.Theme;
		await SelectedThemeChanged.InvokeAsync(item.Theme);
		_isOpen = false;
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
