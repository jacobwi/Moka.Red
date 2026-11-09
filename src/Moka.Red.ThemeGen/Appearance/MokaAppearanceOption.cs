namespace Moka.Red.ThemeGen.Appearance;

/// <summary>How <see cref="MokaAppearancePanel" /> offers one option.</summary>
public enum MokaAppearanceOptionState
{
	/// <summary>Shown and can be picked.</summary>
	Available,

	/// <summary>Shown greyed out, and can't be picked.</summary>
	Disabled,

	/// <summary>Not shown. A row left with nothing to choose hides too.</summary>
	Hidden
}

/// <summary>
///     One option in <see cref="MokaAppearancePanel" />, as <see cref="MokaAppearancePanel.OptionState" /> is asked about
///     it. Match it with a pattern, such as <c>{ Value: MokaColorMode.System }</c> or
///     <c>{ Row: MokaAppearanceRows.Palette, Value: "original" }</c>.
/// </summary>
/// <param name="Row">The row it's in.</param>
/// <param name="Value">
///     The choice: a <see cref="MokaColorMode" />, a <c>MokaDensity</c> or the matching enum of a style row, a palette's or
///     accent's id, or a <see cref="bool" /> in the descriptions row.
/// </param>
public readonly record struct MokaAppearanceOption(MokaAppearanceRows Row, object Value);
