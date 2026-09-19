using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ThemeGen.Editors;

/// <summary>
///     Editor for the core palette colors in a <see cref="MokaPalette" />.
///     Each color shows a swatch, label, native color input, and hex text input. The text input takes
///     #rgb, #rgba, #rrggbb or #rrggbbaa and puts the previous color back for anything else.
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

	// An imported palette can hold anything, and the swatch color goes into a declaration.
	private static string? SwatchStyle(string color) => new StyleBuilder()
		.AddStyle("background-color", CssValues.IsColor(color) ? color.Trim() : null)
		.Build();

	private async Task HandleColorChange(string propertyName, string? value)
	{
		// Dropping the value is enough to reject it: the render that follows the event puts the
		// palette's color back in the field.
		string newColor = value?.Trim() ?? "";
		if (!CssColor.IsHex(newColor))
		{
			return;
		}

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
}
