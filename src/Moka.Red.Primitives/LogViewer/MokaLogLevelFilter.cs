namespace Moka.Red.Primitives.LogViewer;

/// <summary>How the reader of a <see cref="MokaLogViewer" /> picks the levels shown.</summary>
public enum MokaLogLevelFilter
{
	/// <summary>A button per level that turns it on or off, bound through <see cref="MokaLogViewer.Levels" />.</summary>
	Toggles,

	/// <summary>
	///     One pick of the lowest level shown, which shows that level and every worse one, as <c>journalctl -p</c> does.
	///     Bound through <see cref="MokaLogViewer.MinimumLevel" />.
	/// </summary>
	AtLeast
}
