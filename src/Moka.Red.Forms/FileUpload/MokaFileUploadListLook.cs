namespace Moka.Red.Forms.FileUpload;

/// <summary>
///     How the files chosen in a <see cref="MokaFileUpload" /> are listed under its zone. Applies to the
///     <see cref="MokaFileUploadLook.Standard" /> and <see cref="MokaFileUploadLook.Quiet" /> looks; the others draw their
///     files their own way.
/// </summary>
public enum MokaFileUploadListLook
{
	/// <summary>
	///     The look's own list: one bordered row per file with its name, its size and a remove button under the Standard
	///     zone, small chips under the Quiet line. The default.
	/// </summary>
	Standard,

	/// <summary>One grouped list: a colored type tile, the name, the size and type in monospace, and a remove button.</summary>
	Rows,

	/// <summary>
	///     Rows with a thin line under each name for the upload: done with a check, uploading with a percent, failed in red
	///     with Retry. The app reports each file's state through <see cref="MokaFileUpload.Status" />.
	/// </summary>
	Progress,

	/// <summary>Each file as a chip that wraps, with the total and Clear all under them. Takes the least height.</summary>
	Chips,

	/// <summary>Pictures as thumbnails in one row that scrolls, other files as a type tile. Remove shows on hover or focus.</summary>
	Thumbnails
}
