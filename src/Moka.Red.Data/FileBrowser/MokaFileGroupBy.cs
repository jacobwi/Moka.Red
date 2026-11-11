namespace Moka.Red.Data.FileBrowser;

/// <summary>What a <see cref="MokaFileBrowser{TItem}" /> gathers its items under headings by.</summary>
public enum MokaFileGroupBy
{
	/// <summary>No headings. The default.</summary>
	None,

	/// <summary>By kind: folders, images, documents and so on, as <see cref="MokaFileCategory" /> orders them.</summary>
	Kind,

	/// <summary>By when the item last changed, from today back, as <see cref="MokaFileDateGroup" /> orders them.</summary>
	Date
}
