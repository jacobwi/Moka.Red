using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen;

/// <summary>
///     Visual theme editor component with tabbed sections for palette, typography, spacing,
///     presets, and import/export. Includes an optional live preview panel.
/// </summary>
public partial class MokaThemeEditor : ComponentBase
{
	private Tab _activeTab = Tab.Palette;

	/// <summary>The current theme being edited. Two-way bindable.</summary>
	[Parameter]
	public MokaTheme Theme { get; set; } = MokaTheme.Light;

	/// <summary>Fires when the theme changes.</summary>
	[Parameter]
	public EventCallback<MokaTheme> ThemeChanged { get; set; }

	/// <summary>Fires with JSON when the user exports.</summary>
	[Parameter]
	public EventCallback<string> OnExport { get; set; }

	/// <summary>Whether to show the live preview panel. Default true.</summary>
	[Parameter]
	public bool ShowPreview { get; set; } = true;

	/// <summary>Whether to show the import/export tab. Default true.</summary>
	[Parameter]
	public bool ShowImportExport { get; set; } = true;

	/// <summary>Whether to use a compact single-column layout. Default false.</summary>
	[Parameter]
	public bool Compact { get; set; }

	private async Task HandlePaletteChanged(MokaPalette palette)
	{
		Theme = Theme with { Palette = palette };
		await ThemeChanged.InvokeAsync(Theme);
	}

	private async Task HandleTypographyChanged(MokaTypography typography)
	{
		Theme = Theme with { Typography = typography };
		await ThemeChanged.InvokeAsync(Theme);
	}

	private async Task HandleSpacingChanged(MokaSpacing spacing)
	{
		Theme = Theme with { Spacing = spacing };
		await ThemeChanged.InvokeAsync(Theme);
	}

	private async Task HandlePresetSelected(MokaTheme preset)
	{
		Theme = preset;
		await ThemeChanged.InvokeAsync(Theme);
	}

	private async Task HandleImport(MokaTheme imported)
	{
		Theme = imported;
		await ThemeChanged.InvokeAsync(Theme);
	}

	private enum Tab
	{
		Palette,
		Typography,
		Spacing,
		Presets,
		ImportExport
	}
}
