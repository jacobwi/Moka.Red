namespace Moka.Red.Primitives.ColorSwatch;

/// <summary>
///     Every word a <see cref="MokaColorSwatch" /> writes. They name controls that show only a color or an icon, so
///     screen readers read them out and the tooltips show them. Set them to translate the swatch.
/// </summary>
public sealed record MokaColorSwatchText
{
	/// <summary>The swatch <see cref="MokaColorSwatch.ShowNone" /> adds, which chooses no color.</summary>
	public string None { get; init; } = "No color";

	/// <summary>
	///     The word on the no color pill of <see cref="MokaSwatchNoneLook.Chip" />, which is also its name. Default
	///     "None".
	/// </summary>
	public string NoneChip { get; init; } = "None";

	/// <summary>
	///     The + button that opens the custom color input, the panel <see cref="MokaSwatchCustomLook.Rainbow" /> and
	///     <see cref="MokaSwatchCustomLook.Pill" /> open, and the color input in the default look.
	/// </summary>
	public string Custom { get; init; } = "Custom color";

	/// <summary>The word on the pill of <see cref="MokaSwatchCustomLook.Pill" />. Default "Custom".</summary>
	public string CustomPill { get; init; } = "Custom";

	/// <summary>The button that takes the color in the custom input.</summary>
	public string Confirm { get; init; } = "Use this color";

	/// <summary>The button that closes the custom input, or the custom color panel, without a change.</summary>
	public string Cancel { get; init; } = "Cancel";

	/// <summary>The button in the custom color panel that adds the color. Default "Add".</summary>
	public string Add { get; init; } = "Add";

	/// <summary>The browser's color button in the custom color panel. Default "Pick a color".</summary>
	public string PickColor { get; init; } = "Pick a color";

	/// <summary>The hex field in the custom color panel. Default "Hex".</summary>
	public string Hex { get; init; } = "Hex";

	/// <summary>The hex field of <see cref="MokaSwatchCustomLook.Hex" />. Default "Custom color, as hex".</summary>
	public string HexField { get; init; } = "Custom color, as hex";

	/// <summary>The placeholder of the hex field of <see cref="MokaSwatchCustomLook.Hex" />. Default "#hex".</summary>
	public string HexPlaceholder { get; init; } = "#hex";

	/// <summary>What a screen reader hears when a hex field holds no color on Enter. Default "Six hex digits".</summary>
	public string HexInvalid { get; init; } = "Six hex digits";
}
