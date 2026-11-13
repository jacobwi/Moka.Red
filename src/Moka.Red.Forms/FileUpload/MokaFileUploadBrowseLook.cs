namespace Moka.Red.Forms.FileUpload;

/// <summary>
///     How the drop zone of a <see cref="MokaFileUpload" /> draws its Browse word, in the
///     <see cref="MokaFileUploadLook.Standard" /> look (compact or not) and the <see cref="MokaFileUploadLook.CardList" />
///     look. Either way it is a picture of a button: the file input covers the zone, so a click anywhere on it, Enter or
///     Space on the focused input, and an app's file picker all work the same.
/// </summary>
public enum MokaFileUploadBrowseLook
{
	/// <summary>An accent link in the zone's sentence. The default.</summary>
	Link,

	/// <summary>An outlined button: under the zone's words, at the end of a card list's row, or alone in a compact zone.</summary>
	Button
}
