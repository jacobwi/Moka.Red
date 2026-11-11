namespace Moka.Red.Data.FileBrowser;

/// <summary>The broad kind of an item, which decides its heading under <see cref="MokaFileGroupBy.Kind" /> and its type icon.</summary>
public enum MokaFileCategory
{
	/// <summary>A folder.</summary>
	Folder,

	/// <summary>A picture.</summary>
	Image,

	/// <summary>Text, a PDF, a spreadsheet or another document.</summary>
	Document,

	/// <summary>Source code or a data file such as JSON.</summary>
	Code,

	/// <summary>Sound.</summary>
	Audio,

	/// <summary>A film.</summary>
	Video,

	/// <summary>A packed file such as a zip.</summary>
	Archive,

	/// <summary>A log.</summary>
	Log,

	/// <summary>Anything else.</summary>
	Other
}
