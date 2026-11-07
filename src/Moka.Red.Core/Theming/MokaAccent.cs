namespace Moka.Red.Core.Theming;

/// <summary>An accent in one color scheme.</summary>
/// <param name="Color">Primary buttons, focus rings and selected borders.</param>
/// <param name="Soft">Links and selected text on that scheme's surfaces.</param>
/// <param name="Background">The tint behind something selected.</param>
public sealed record MokaAccentShades(string Color, string Soft, string Background);

/// <summary>
///     An accent for the <see cref="MokaLook.Gate" /> look, with its shades for each color scheme. In this look
///     green, amber and red mean allowed, limited and blocked, so of those only <see cref="Red" /> is a preset:
///     on it, a primary button and an error share a color and stay apart by their icons and words.
/// </summary>
public sealed record MokaAccent(string Id, string Name, MokaAccentShades Dark, MokaAccentShades Light)
{
	/// <summary>The default accent of <see cref="MokaTheme.Gate" />.</summary>
	public static MokaAccent Blue { get; } = new("blue", "Blue",
		new MokaAccentShades("#6E92FF", "#A9C0FF", "#172243"),
		new MokaAccentShades("#2F58D6", "#2A4FC2", "#E6ECFE"));

	/// <summary>A violet preset.</summary>
	public static MokaAccent Violet { get; } = new("violet", "Violet",
		new MokaAccentShades("#8F7DFF", "#B3A8FF", "#1E1A3A"),
		new MokaAccentShades("#5B45E0", "#4E37D2", "#ECE9FF"));

	/// <summary>A cyan preset.</summary>
	public static MokaAccent Cyan { get; } = new("cyan", "Cyan",
		new MokaAccentShades("#3CC8E6", "#9EE2F2", "#10303A"),
		new MokaAccentShades("#00748C", "#006980", "#DDF3F8"));

	/// <summary>An orchid preset.</summary>
	public static MokaAccent Orchid { get; } = new("orchid", "Orchid",
		new MokaAccentShades("#D57CF2", "#E8B6F8", "#2B1A36"),
		new MokaAccentShades("#9330B8", "#8428A8", "#F5E8FB"));

	/// <summary>A pink preset.</summary>
	public static MokaAccent Pink { get; } = new("pink", "Pink",
		new MokaAccentShades("#F27BCF", "#F8B9E6", "#361A30"),
		new MokaAccentShades("#B02A88", "#A0237A", "#FBE7F4"));

	/// <summary>A silver preset.</summary>
	public static MokaAccent Silver { get; } = new("silver", "Silver",
		new MokaAccentShades("#B5BFD0", "#D6DCE7", "#222A38"),
		new MokaAccentShades("#4C576B", "#434D60", "#E8ECF2"));

	/// <summary>The accent of <see cref="MokaTheme.Red" />, the library's default theme.</summary>
	public static MokaAccent Red { get; } = new("red", "Red",
		new MokaAccentShades("#EF5350", "#FF9D9A", "#2E1516"),
		new MokaAccentShades("#C62828", "#B71C1C", "#FCE8E8"));

	/// <summary>Every preset, the default of <see cref="MokaTheme.Gate" /> first.</summary>
	public static IReadOnlyList<MokaAccent> Presets { get; } = [Blue, Violet, Cyan, Orchid, Pink, Silver, Red];

	/// <summary>
	///     An accent from a hue and an HSL saturation. Each shade is found by its luminance, so any hue and
	///     saturation meets the same contrast targets as the presets in both color schemes. Saturation is capped
	///     per shade, so a muted color stays muted and a vivid one gets the presets' strength.
	/// </summary>
	/// <param name="hue">The hue in degrees. Any value, wrapped into 0 to 360.</param>
	/// <param name="saturation">The HSL saturation, 0 to 1.</param>
	public static MokaAccent FromHue(double hue, double saturation = 1)
	{
		double wrappedHue = ((hue % 360) + 360) % 360;
		double clamped = Math.Clamp(saturation, 0, 1);

		string Shade(double cap, double luminance) =>
			MokaColorMath.Shade(wrappedHue, Math.Min(clamped, cap), luminance);

		return new MokaAccent("custom", "Custom",
			new MokaAccentShades(Shade(0.82, 0.30), Shade(0.90, 0.56), Shade(0.42, 0.016)),
			new MokaAccentShades(Shade(0.72, 0.12), Shade(0.75, 0.095), Shade(0.90, 0.86)));
	}

	/// <summary>
	///     An accent from a color's hue and saturation (see <see cref="FromHue" />). The shades take that hue and
	///     saturation, not the color itself, so the color you pass may not appear in them. A preset's own color
	///     gives that preset.
	/// </summary>
	/// <param name="color">A hex color, <c>#rgb</c> or <c>#rrggbb</c>.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="color" /> is not a hex color.</exception>
	public static MokaAccent FromColor(string color)
	{
		if (!MokaColorMath.TryParseHex(color, out int r, out int g, out int b))
		{
			throw new ArgumentException($"An accent must be a hex color like #6e92ff, got '{color}'.", nameof(color));
		}

		foreach (MokaAccent preset in Presets)
		{
			if (string.Equals(preset.Dark.Color, color, StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(preset.Light.Color, color, StringComparison.OrdinalIgnoreCase))
			{
				return preset;
			}
		}

		(double hue, double saturation) = MokaColorMath.ToHueSaturation(r, g, b);
		return FromHue(hue, saturation);
	}

	/// <summary>The shades for one color scheme.</summary>
	public MokaAccentShades For(bool isDark) => isDark ? Dark : Light;
}
