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
	private MokaTheme? _lastTheme;
	private MokaTheme _theme = MokaTheme.Light;

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

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Theme only seeds the editor when the parent passes a different theme, so a parent render
		// cannot throw away the user's edits. Themes are records: a fresh MokaTheme.Light on every
		// parent render compares equal to the last one.
		if (!EqualityComparer<MokaTheme?>.Default.Equals(_lastTheme, Theme))
		{
			_lastTheme = Theme;
			_theme = Theme;
		}
	}

	private Task HandlePaletteChanged(MokaPalette palette) => ApplyAsync(_theme with { Palette = palette });

	private Task HandleTypographyChanged(MokaTypography typography) =>
		ApplyAsync(_theme with { Typography = typography });

	private Task HandleSpacingChanged(MokaSpacing spacing) => ApplyAsync(_theme with { Spacing = spacing });

	private Task HandlePresetSelected(MokaTheme preset) => ApplyAsync(preset);

	private Task HandleImport(MokaTheme imported) => ApplyAsync(imported);

	private async Task ApplyAsync(MokaTheme theme)
	{
		_theme = theme;
		await ThemeChanged.InvokeAsync(theme);
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
