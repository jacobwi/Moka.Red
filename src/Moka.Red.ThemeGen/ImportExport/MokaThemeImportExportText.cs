namespace Moka.Red.ThemeGen.ImportExport;

/// <summary>Every word <see cref="MokaThemeImportExport" /> writes. Change any with <c>new MokaThemeImportExportText { ... }</c>.</summary>
public sealed record MokaThemeImportExportText
{
	/// <summary>The export part's heading.</summary>
	public string ExportHeading { get; init; } = "Export";

	/// <summary>The accessible name of the format choice.</summary>
	public string FormatLabel { get; init; } = "Format";

	/// <summary>The C# format's name.</summary>
	public string CSharp { get; init; } = "C#";

	/// <summary>The JSON format's name.</summary>
	public string Json { get; init; } = "JSON";

	/// <summary>The CSS format's name.</summary>
	public string Css { get; init; } = "CSS";

	/// <summary>A line under the C# export saying what it is.</summary>
	public string CSharpHint { get; init; } = "Paste it where the app builds its theme.";

	/// <summary>A line under the JSON export saying what it is.</summary>
	public string JsonHint { get; init; } = "Store it, and read it back with MokaThemeSerializer.FromJson.";

	/// <summary>A line under the CSS export saying what it is.</summary>
	public string CssHint { get; init; } = "For a page without a theme provider: put the root classes on the element that holds the app.";

	/// <summary>The button that hands the JSON to the app, shown when it listens.</summary>
	public string Export { get; init; } = "Export";

	/// <summary>The import part's heading.</summary>
	public string ImportHeading { get; init; } = "Import JSON";

	/// <summary>The import field's label.</summary>
	public string ImportLabel { get; init; } = "Theme JSON";

	/// <summary>The import field's placeholder.</summary>
	public string ImportPlaceholder { get; init; } = "Paste theme JSON here";

	/// <summary>The button that applies the pasted theme.</summary>
	public string Apply { get; init; } = "Apply";

	/// <summary>The error shown when Apply is pressed with nothing pasted.</summary>
	public string ImportEmpty { get; init; } = "Paste a theme's JSON first.";
}
