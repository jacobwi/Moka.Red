namespace Moka.Red.ThemeGen.ImportExport;

/// <summary>A format <see cref="MokaThemeImportExport" /> writes a theme in.</summary>
public enum MokaThemeExportFormat
{
	/// <summary>C# that builds the theme back, written with the <c>With</c> API where it can be.</summary>
	CSharp,

	/// <summary>JSON that <c>MokaThemeSerializer.FromJson</c> reads back into an equal theme.</summary>
	Json,

	/// <summary>A stylesheet with the theme's tokens, its <c>color-scheme</c> and the root classes it needs.</summary>
	Css
}
