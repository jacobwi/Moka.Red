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
		new("Ocean", new MokaTheme
		{
			IsDark = false,
			Palette = new MokaPalette
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
			}
		}),
		new("Forest", new MokaTheme
		{
			IsDark = false,
			Palette = new MokaPalette
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
			}
		}),
		new("Sunset", new MokaTheme
		{
			IsDark = false,
			Palette = new MokaPalette
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
			}
		}),
		new("Midnight", new MokaTheme
		{
			IsDark = true,
			Palette = new MokaPalette
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
			}
		}),
		new("Rose", new MokaTheme
		{
			IsDark = false,
			Palette = new MokaPalette
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
			}
		}),
		new("Monochrome", new MokaTheme
		{
			IsDark = false,
			Palette = new MokaPalette
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
			}
		})
	];

	/// <summary>Fires when a preset is selected.</summary>
	[Parameter]
	public EventCallback<MokaTheme> OnPresetSelected { get; set; }

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task ApplyPreset(MokaTheme theme) => await OnPresetSelected.InvokeAsync(theme);

	private readonly record struct PresetEntry(string Name, MokaTheme Theme);
}
