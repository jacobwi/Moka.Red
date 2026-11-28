namespace Moka.Red.Core.Theming;

/// <summary>Which files of Inter, Space Grotesk and JetBrains Mono the outermost <see cref="MokaThemeProvider" /> links.</summary>
public enum MokaFontFiles
{
	/// <summary>One variable file per font and script, <c>moka-fonts.css</c>. The default, and the fewest bytes on screen.</summary>
	Variable,

	/// <summary>
	///     A file per weight from 300 to 700, <c>moka-fonts-static.css</c>. Chromium can't embed a variable font in a PDF, so
	///     printing the variable files gives Type 3 glyphs; these embed as ordinary fonts, which PDF tools, text extractors
	///     and applicant tracking systems read. For an app that prints or saves pages as PDFs. A weight outside 300 to 700
	///     takes the nearest one.
	/// </summary>
	Static
}
