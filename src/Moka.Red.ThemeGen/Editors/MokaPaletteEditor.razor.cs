using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Editors;

/// <summary>
///     Editor for all palette colors in a <see cref="MokaPalette" />.
///     Each color shows a swatch, label, native color input, and hex text input.
/// </summary>
public partial class MokaPaletteEditor : ComponentBase
{
	/// <summary>The palette being edited.</summary>
	[Parameter]
	public MokaPalette Palette { get; set; } = MokaPalette.Light;

	/// <summary>Fires when any palette color changes.</summary>
	[Parameter]
	public EventCallback<MokaPalette> PaletteChanged { get; set; }

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleColorChange(string propertyName, string newColor)
	{
		MokaPalette updated = propertyName switch
		{
			"Primary" => Palette with { Primary = newColor },
			"PrimaryLight" => Palette with { PrimaryLight = newColor },
			"PrimaryDark" => Palette with { PrimaryDark = newColor },
			"OnPrimary" => Palette with { OnPrimary = newColor },
			"Secondary" => Palette with { Secondary = newColor },
			"SecondaryLight" => Palette with { SecondaryLight = newColor },
			"SecondaryDark" => Palette with { SecondaryDark = newColor },
			"OnSecondary" => Palette with { OnSecondary = newColor },
			"Surface" => Palette with { Surface = newColor },
			"SurfaceVariant" => Palette with { SurfaceVariant = newColor },
			"OnSurface" => Palette with { OnSurface = newColor },
			"Background" => Palette with { Background = newColor },
			"OnBackground" => Palette with { OnBackground = newColor },
			"Error" => Palette with { Error = newColor },
			"OnError" => Palette with { OnError = newColor },
			"Warning" => Palette with { Warning = newColor },
			"OnWarning" => Palette with { OnWarning = newColor },
			"Success" => Palette with { Success = newColor },
			"OnSuccess" => Palette with { OnSuccess = newColor },
			"Info" => Palette with { Info = newColor },
			"OnInfo" => Palette with { OnInfo = newColor },
			"Outline" => Palette with { Outline = newColor },
			"OutlineVariant" => Palette with { OutlineVariant = newColor },
			_ => Palette
		};

		await PaletteChanged.InvokeAsync(updated);
	}

	private static string FormatLabel(string propertyName) => Regex.Replace(propertyName, "(?<=[a-z])([A-Z])", " $1");

	private static string NormalizeHex(string color)
	{
		// Native color input only accepts 7-char hex (#rrggbb)
		if (color.StartsWith('#') && color.Length == 7)
		{
			return color;
		}

		if (color.StartsWith('#') && color.Length == 4)
		{
			return $"#{color[1]}{color[1]}{color[2]}{color[2]}{color[3]}{color[3]}";
		}

		return color.StartsWith('#') ? color[..7] : $"#{color}";
	}
}
