namespace Moka.Red.ThemeGen.Appearance;

/// <summary>
///     One swatch in the neutral row of <see cref="MokaAppearancePanel" />: a color whose hue and strength tint the
///     surfaces, borders and greys. Its lightness doesn't matter, so the presets are drawn mid-grey where the tint shows.
/// </summary>
/// <param name="Name">The swatch's name, read out and shown in its tooltip.</param>
/// <param name="Color">The color, as <see cref="MokaAppearance.CustomNeutral" /> holds it.</param>
public sealed record MokaAppearanceNeutral(string Name, string Color)
{
	/// <summary>No tint at all: pure greys.</summary>
	public static MokaAppearanceNeutral Grey { get; } = new("Grey", "#8C8C8C");

	/// <summary>A cool blue-grey.</summary>
	public static MokaAppearanceNeutral Slate { get; } = new("Slate", "#7A8494");

	/// <summary>A warm grey.</summary>
	public static MokaAppearanceNeutral Stone { get; } = new("Stone", "#8E857A");

	/// <summary>A green-grey.</summary>
	public static MokaAppearanceNeutral Sage { get; } = new("Sage", "#7D8A7F");

	/// <summary>A violet-grey.</summary>
	public static MokaAppearanceNeutral Plum { get; } = new("Plum", "#887D8E");

	/// <summary>The neutrals offered unless an app passes its own.</summary>
	public static IReadOnlyList<MokaAppearanceNeutral> Presets { get; } = [Grey, Slate, Stone, Sage, Plum];
}
