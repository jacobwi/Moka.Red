namespace Moka.Red.Core.Enums;

/// <summary>How text that runs over more than one line breaks, the CSS <c>text-wrap</c> style.</summary>
public enum MokaTextWrap
{
	/// <summary>As the browser breaks text by default: as many words on each line as fit.</summary>
	Normal,

	/// <summary>
	///     Breaks so the last line isn't a single word, for running text of a few lines. A browser that doesn't know it
	///     breaks as <see cref="Normal" /> does.
	/// </summary>
	Pretty,

	/// <summary>
	///     Evens out the lengths of the lines, for short text such as a title or a line or two under it. A browser that
	///     doesn't know it breaks as <see cref="Normal" /> does.
	/// </summary>
	Balance
}
