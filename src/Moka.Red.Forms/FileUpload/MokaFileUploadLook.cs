namespace Moka.Red.Forms.FileUpload;

/// <summary>How a <see cref="MokaFileUpload" /> draws itself and the files chosen in it.</summary>
public enum MokaFileUploadLook
{
	/// <summary>A dashed zone with an upload icon and a Browse link, and the chosen files listed under it. The default.</summary>
	Standard,

	/// <summary>A slimmer zone, then each file as a small card with its type, its size and, when the app reports it, its upload progress.</summary>
	CardList,

	/// <summary>Sized like a text field: a Browse button with the chosen files as removable chips inside it. A drop on the field adds files.</summary>
	Field,

	/// <summary>
	///     One picture in a round or rounded-square frame, for an avatar or a logo. Replace shows on hover or focus, and a
	///     drop on the frame replaces the picture. Takes one file.
	/// </summary>
	Picture,

	/// <summary>Picture thumbnails in a grid with an Add tile at the end. The first is the cover, and remove shows on hover or focus.</summary>
	Gallery,

	/// <summary>One line under the fields, for a tight form where a big zone would push everything down. A drop on the line adds files.</summary>
	Quiet,

	/// <summary>Monospace in a dashed accent frame, each file a log line with a text bar for its progress.</summary>
	Terminal,

	/// <summary>One required document, empty or filled, with Replace and Remove. Takes one file.</summary>
	Slot
}
