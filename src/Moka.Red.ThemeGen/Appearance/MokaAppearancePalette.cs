using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Appearance;

/// <summary>
///     One choice in the palette row of <see cref="MokaAppearancePanel" />: a dark theme and a light theme under one name.
///     An app makes its own for a brand palette, and <c>with { Name = ... }</c> renames a built-in one.
/// </summary>
/// <param name="Id">What a stored <see cref="MokaAppearance.Palette" /> holds, so keep it stable.</param>
/// <param name="Name">The name on the card.</param>
/// <param name="Dark">The theme in dark mode.</param>
/// <param name="Light">The theme in light mode.</param>
public sealed record MokaAppearancePalette(string Id, string Name, MokaTheme Dark, MokaTheme Light)
{
	/// <summary>A line under the name on the card.</summary>
	public string? Hint { get; init; }

	/// <summary>Whether the style choices, density and text size among them, do anything on this palette.</summary>
	public bool TakesStyleChoices => Dark.Look == MokaLook.Gate || Light.Look == MokaLook.Gate;

	/// <summary>Graphite with a red accent, the library's default.</summary>
	public static MokaAppearancePalette Red { get; } = new("red", "Red", MokaTheme.Red, MokaTheme.RedLight)
	{
		Hint = "Graphite with a red accent"
	};

	/// <summary>Blue-grey with a blue accent.</summary>
	public static MokaAppearancePalette Gate { get; } = new("gate", "Gate", MokaTheme.Gate, MokaTheme.GateLight)
	{
		Hint = "Blue-grey with a blue accent"
	};

	/// <summary>
	///     The colors of <see cref="Gate" /> with its style choices at their tightest. Not in <see cref="Presets" />:
	///     as a picture it looks just like Gate, so it's for an app that wants those starting choices.
	/// </summary>
	public static MokaAppearancePalette Crisp { get; } = new("crisp", "Crisp", MokaTheme.Crisp, MokaTheme.CrispLight)
	{
		Hint = "Blue-grey, small and tight"
	};

	/// <summary>Near-black with red-tinted lines.</summary>
	public static MokaAppearancePalette Matrix { get; } = new("matrix", "Matrix", MokaTheme.Matrix, MokaTheme.MatrixLight)
	{
		Hint = "Near-black with red-tinted lines"
	};

	/// <summary>The library's first design, exactly as it has always rendered. It takes an accent but no style choices.</summary>
	public static MokaAppearancePalette Original { get; } = new("original", "Original", MokaTheme.Dark, MokaTheme.Light)
	{
		Hint = "The first design, with its style fixed"
	};

	/// <summary>The palettes offered unless an app passes its own: <see cref="Red" />, <see cref="Gate" />, <see cref="Matrix" /> and <see cref="Original" />.</summary>
	public static IReadOnlyList<MokaAppearancePalette> Presets { get; } = [Red, Gate, Matrix, Original];
}
