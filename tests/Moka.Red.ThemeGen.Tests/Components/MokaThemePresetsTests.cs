using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Presets;

namespace Moka.Red.ThemeGen.Tests.Components;

// The six extra presets set only the 23 core colors. Every other palette color kept its MokaPalette
// initializer value, which is the dark matrix one: the light presets got near-black hover and surface
// colors, and all six kept the red glow and border tokens.
public class MokaThemePresetsTests : BunitContext
{
	private static readonly string[] BuiltInPresets = ["Moka Light", "Moka Dark"];

	public static TheoryData<string> PresetNames =>
		["Moka Light", "Moka Dark", "Ocean", "Forest", "Sunset", "Midnight", "Rose", "Monochrome"];

	[Fact]
	public void OffersTheDocumentedPresets() =>
		Assert.Equal(PresetNames.Select(row => row.Data), Presets().Keys);

	[Theory]
	[MemberData(nameof(PresetNames))]
	public void GlowAndBorderTokens_AreThePresetsPrimary(string name)
	{
		MokaPalette palette = Presets()[name].Palette;
		(int R, int G, int B) primary = Rgb(palette.Primary);

		foreach (PropertyInfo property in ThemeReflection.PublicProperties(typeof(MokaPalette))
			         .Where(p => p.Name.StartsWith("PrimaryGlow", StringComparison.Ordinal) ||
			                     p.Name.StartsWith("PrimaryBorder", StringComparison.Ordinal)))
		{
			Assert.True(primary == Rgb((string)property.GetValue(palette)!),
				$"{name}: {property.Name} is {property.GetValue(palette)}, not a tint of {palette.Primary}.");
		}
	}

	[Theory]
	[MemberData(nameof(PresetNames))]
	public void DimFills_AreThePresetsSemanticColors(string name)
	{
		MokaPalette palette = Presets()[name].Palette;

		foreach (PropertyInfo dim in ThemeReflection.PublicProperties(typeof(MokaPalette))
			         .Where(p => p.Name.EndsWith("Dim", StringComparison.Ordinal) && p.Name != "PrimaryBorderDim"))
		{
			string colorName = dim.Name[..^"Dim".Length];
			string color = (string)typeof(MokaPalette).GetProperty(colorName)!.GetValue(palette)!;
			Assert.True(Rgb(color) == Rgb((string)dim.GetValue(palette)!),
				$"{name}: {dim.Name} is {dim.GetValue(palette)}, not a tint of {colorName} {color}.");
		}
	}

	[Theory]
	[MemberData(nameof(PresetNames))]
	public void ExtendedSurfaces_StepFromTheSurfaceTowardTheText(string name)
	{
		MokaPalette palette = Presets()[name].Palette;
		double surface = Luminance(palette.Surface);
		double text = Luminance(palette.OnSurface);

		foreach (string step in new[] { palette.SurfaceHover, palette.Surface2, palette.Surface3 })
		{
			Assert.True(Contrast(step, palette.Surface) < 1.5, $"{name}: {step} is far from the surface {palette.Surface}.");
			Assert.True(Math.Sign(Luminance(step) - surface) == Math.Sign(text - surface),
				$"{name}: {step} moves away from the text color instead of toward it.");
		}
	}

	[Theory]
	[MemberData(nameof(PresetNames))]
	public void TextSteps_GetQuieterInOrder(string name)
	{
		MokaPalette palette = Presets()[name].Palette;
		double[] contrasts =
		[
			Contrast(palette.OnSurface, palette.Surface),
			Contrast(palette.OnSurfaceVariant, palette.Surface),
			Contrast(palette.OnSurfaceTertiary, palette.Surface),
			Contrast(palette.OnSurfaceQuaternary, palette.Surface),
			1
		];

		for (int i = 1; i < contrasts.Length; i++)
		{
			Assert.True(contrasts[i - 1] > contrasts[i],
				$"{name}: text step {i} has more contrast than step {i - 1} ({string.Join(", ", contrasts)}).");
		}
	}

	[Theory]
	[MemberData(nameof(PresetNames))]
	public void ExtraPresets_SetEveryPaletteColor(string name)
	{
		if (BuiltInPresets.Contains(name))
		{
			return;
		}

		// A palette color a preset leaves out keeps its initializer value. Reflection finds the
		// initializer values, so a color added to MokaPalette later is checked too.
		MokaPalette initializerValues = Activator.CreateInstance<MokaPalette>();
		MokaPalette palette = Presets()[name].Palette;

		foreach (PropertyInfo property in ThemeReflection.PublicProperties(typeof(MokaPalette))
			         .Where(p => p.GetCustomAttribute<RequiredMemberAttribute>() is null))
		{
			Assert.True(!Equals(property.GetValue(initializerValues), property.GetValue(palette)),
				$"{name}: {property.Name} kept the MokaPalette initializer value {property.GetValue(palette)}.");
		}
	}

	private Dictionary<string, MokaTheme> Presets()
	{
		MokaTheme? selected = null;
		IRenderedComponent<MokaThemePresets> cut = Render<MokaThemePresets>(p => p
			.Add(x => x.OnPresetSelected, EventCallback.Factory.Create<MokaTheme>(this, t => selected = t)));

		var presets = new Dictionary<string, MokaTheme>();
		int count = cut.FindAll(".moka-theme-presets__card").Count;
		for (int i = 0; i < count; i++)
		{
			// Found again each time: a click re-renders the grid and retires the old handler ids.
			IElement card = cut.FindAll(".moka-theme-presets__card")[i];
			string name = card.QuerySelector(".moka-theme-presets__name")!.TextContent.Trim();
			card.Click();
			presets[name] = selected!;
		}

		return presets;
	}

	// ── Color math ────────────────────────────────────────────────

	private static (int R, int G, int B) Rgb(string color)
	{
		color = color.Trim();
		if (color.StartsWith('#') && color.Length == 7)
		{
			return (Hex(color, 1), Hex(color, 3), Hex(color, 5));
		}

		if (color.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
		{
			string[] parts = color[(color.IndexOf('(', StringComparison.Ordinal) + 1)..^1].Split(',');
			return (Channel(parts[0]), Channel(parts[1]), Channel(parts[2]));
		}

		throw new FormatException($"Cannot read the color '{color}'.");

		static int Hex(string value, int start) =>
			int.Parse(value.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

		static int Channel(string value) => int.Parse(value.Trim(), CultureInfo.InvariantCulture);
	}

	// WCAG relative luminance and contrast ratio.
	private static double Luminance(string color)
	{
		(int r, int g, int b) = Rgb(color);
		return (0.2126 * Linear(r)) + (0.7152 * Linear(g)) + (0.0722 * Linear(b));

		static double Linear(int channel)
		{
			double c = channel / 255.0;
			return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
		}
	}

	private static double Contrast(string first, string second)
	{
		double a = Luminance(first), b = Luminance(second);
		return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
	}
}
