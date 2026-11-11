namespace Moka.Red.Core.Enums;

/// <summary>
///     Which theme colors draw the pattern of a <c>MokaGridBackground</c>. A <c>PatternColor</c> set by the app
///     wins over any tone.
/// </summary>
public enum MokaGridTone
{
	/// <summary>The accent: the primary border token, the original look.</summary>
	Accent,

	/// <summary>Lines in the outline color, so the accent shows only in the glow.</summary>
	Neutral,

	/// <summary>
	///     Neutral minor lines with the accent on the major lines. Only <c>GraphPaper</c> has major lines; on any
	///     other pattern this draws like <see cref="Accent" />.
	/// </summary>
	AccentMajor,

	/// <summary>The text color at low strength. Reads as pencil on any theme.</summary>
	Text
}
