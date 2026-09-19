using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Presets;

/// <summary>
///     Grid of built-in theme presets. Click to apply a preset theme.
/// </summary>
public partial class MokaThemePresets : ComponentBase
{
	private static readonly PresetEntry[] _presets =
	[
		new("Moka Light", MokaTheme.Light),
		new("Moka Dark", MokaTheme.Dark),
		new("Ocean", Complete(false, new MokaPalette
		{
			Primary = "#0277bd",
			PrimaryLight = "#58a5f0",
			PrimaryDark = "#004c8c",
			OnPrimary = "#ffffff",
			Secondary = "#00796b",
			SecondaryLight = "#48a999",
			SecondaryDark = "#004c40",
			OnSecondary = "#ffffff",
			Surface = "#ffffff",
			SurfaceVariant = "#e0f7fa",
			OnSurface = "#1c1b1f",
			Background = "#f5f9fc",
			OnBackground = "#1c1b1f",
			Error = "#b00020",
			OnError = "#ffffff",
			Warning = "#f57c00",
			OnWarning = "#ffffff",
			Success = "#2e7d32",
			OnSuccess = "#ffffff",
			Info = "#0288d1",
			OnInfo = "#ffffff",
			Outline = "#b0bec5",
			OutlineVariant = "#cfd8dc"
		})),
		new("Forest", Complete(false, new MokaPalette
		{
			Primary = "#2e7d32",
			PrimaryLight = "#60ad5e",
			PrimaryDark = "#005005",
			OnPrimary = "#ffffff",
			Secondary = "#5d4037",
			SecondaryLight = "#8b6b61",
			SecondaryDark = "#321911",
			OnSecondary = "#ffffff",
			Surface = "#ffffff",
			SurfaceVariant = "#e8f5e9",
			OnSurface = "#1c1b1f",
			Background = "#f5f8f5",
			OnBackground = "#1c1b1f",
			Error = "#b00020",
			OnError = "#ffffff",
			Warning = "#f57c00",
			OnWarning = "#ffffff",
			Success = "#2e7d32",
			OnSuccess = "#ffffff",
			Info = "#0288d1",
			OnInfo = "#ffffff",
			Outline = "#a5d6a7",
			OutlineVariant = "#c8e6c9"
		})),
		new("Sunset", Complete(false, new MokaPalette
		{
			Primary = "#e65100",
			PrimaryLight = "#ff833a",
			PrimaryDark = "#ac1900",
			OnPrimary = "#ffffff",
			Secondary = "#bf360c",
			SecondaryLight = "#f9683a",
			SecondaryDark = "#870000",
			OnSecondary = "#ffffff",
			Surface = "#ffffff",
			SurfaceVariant = "#fff3e0",
			OnSurface = "#1c1b1f",
			Background = "#fffaf5",
			OnBackground = "#1c1b1f",
			Error = "#b00020",
			OnError = "#ffffff",
			Warning = "#f57c00",
			OnWarning = "#ffffff",
			Success = "#2e7d32",
			OnSuccess = "#ffffff",
			Info = "#0288d1",
			OnInfo = "#ffffff",
			Outline = "#ffcc80",
			OutlineVariant = "#ffe0b2"
		})),
		new("Midnight", Complete(true, new MokaPalette
		{
			Primary = "#7b1fa2",
			PrimaryLight = "#ae52d4",
			PrimaryDark = "#4a0072",
			OnPrimary = "#ffffff",
			Secondary = "#512da8",
			SecondaryLight = "#8559da",
			SecondaryDark = "#140078",
			OnSecondary = "#ffffff",
			Surface = "#1a1a2e",
			SurfaceVariant = "#242445",
			OnSurface = "#e0e0e0",
			Background = "#0f0f23",
			OnBackground = "#e0e0e0",
			Error = "#cf6679",
			OnError = "#000000",
			Warning = "#ffb74d",
			OnWarning = "#000000",
			Success = "#66bb6a",
			OnSuccess = "#000000",
			Info = "#4fc3f7",
			OnInfo = "#000000",
			Outline = "#3a3a5c",
			OutlineVariant = "#2a2a4a"
		})),
		new("Rose", Complete(false, new MokaPalette
		{
			Primary = "#c2185b",
			PrimaryLight = "#fa5788",
			PrimaryDark = "#8c0032",
			OnPrimary = "#ffffff",
			Secondary = "#ad1457",
			SecondaryLight = "#e35183",
			SecondaryDark = "#78002e",
			OnSecondary = "#ffffff",
			Surface = "#ffffff",
			SurfaceVariant = "#fce4ec",
			OnSurface = "#1c1b1f",
			Background = "#fdf5f7",
			OnBackground = "#1c1b1f",
			Error = "#b00020",
			OnError = "#ffffff",
			Warning = "#f57c00",
			OnWarning = "#ffffff",
			Success = "#2e7d32",
			OnSuccess = "#ffffff",
			Info = "#0288d1",
			OnInfo = "#ffffff",
			Outline = "#f48fb1",
			OutlineVariant = "#f8bbd0"
		})),
		new("Monochrome", Complete(false, new MokaPalette
		{
			Primary = "#424242",
			PrimaryLight = "#6d6d6d",
			PrimaryDark = "#1b1b1b",
			OnPrimary = "#ffffff",
			Secondary = "#616161",
			SecondaryLight = "#8e8e8e",
			SecondaryDark = "#373737",
			OnSecondary = "#ffffff",
			Surface = "#ffffff",
			SurfaceVariant = "#f5f5f5",
			OnSurface = "#212121",
			Background = "#fafafa",
			OnBackground = "#212121",
			Error = "#b00020",
			OnError = "#ffffff",
			Warning = "#f57c00",
			OnWarning = "#ffffff",
			Success = "#2e7d32",
			OnSuccess = "#ffffff",
			Info = "#0288d1",
			OnInfo = "#ffffff",
			Outline = "#bdbdbd",
			OutlineVariant = "#e0e0e0"
		}))
	];

	/// <summary>Fires when a preset is selected.</summary>
	[Parameter]
	public EventCallback<MokaTheme> OnPresetSelected { get; set; }

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task ApplyPreset(MokaTheme theme) => await OnPresetSelected.InvokeAsync(theme);

	/// <summary>
	///     Fills in the palette colors a preset does not list, from the preset's own colors. Left to the
	///     <see cref="MokaPalette" /> initializers they are the dark matrix values, which gave light presets
	///     near-black hover and surface colors and every preset a red glow.
	/// </summary>
	private static MokaTheme Complete(bool isDark, MokaPalette core)
	{
		// WithAccent owns the glow and border tiers. It also rewrites PrimaryLight, PrimaryDark and the
		// dark outlines, so only the tokens below are taken from it and the preset keeps its own.
		MokaPalette accent = (isDark ? MokaTheme.Dark : MokaTheme.Light).WithAccent(core.Primary).Palette;

		return new MokaTheme
		{
			IsDark = isDark,
			Palette = core with
			{
				// Surfaces step toward the text color, as the built-in palettes do: a raise on dark
				// themes, a shade on light ones.
				SurfaceHover = CssColor.Mix(core.Surface, core.OnSurface, 0.05),
				Surface2 = CssColor.Mix(core.Surface, core.OnSurface, 0.025),
				Surface3 = CssColor.Mix(core.Surface, core.OnSurface, 0.075),

				// Secondary text steps fade toward the surface, each one quieter than the last.
				OnSurfaceVariant = CssColor.Mix(core.OnSurface, core.Surface, 0.3),
				OnSurfaceTertiary = CssColor.Mix(core.OnSurface, core.Surface, 0.5),
				OnSurfaceQuaternary = CssColor.Mix(core.OnSurface, core.Surface, 0.7),

				// The alpha tiers of MokaPalette.Light and MokaPalette.Dark.
				ErrorDim = CssColor.WithAlpha(core.Error, isDark ? 0.12 : 0.10),
				WarningDim = CssColor.WithAlpha(core.Warning, 0.12),
				SuccessDim = CssColor.WithAlpha(core.Success, isDark ? 0.15 : 0.12),
				InfoDim = CssColor.WithAlpha(core.Info, isDark ? 0.15 : 0.12),

				PrimaryGlow = accent.PrimaryGlow,
				PrimaryGlowMd = accent.PrimaryGlowMd,
				PrimaryGlowStrong = accent.PrimaryGlowStrong,
				PrimaryGlowFaint = accent.PrimaryGlowFaint,
				PrimaryBorder = accent.PrimaryBorder,
				PrimaryBorderDim = accent.PrimaryBorderDim
			}
		};
	}

	private readonly record struct PresetEntry(string Name, MokaTheme Theme);
}
