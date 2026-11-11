namespace Moka.Red.Forms.FileUpload;

/// <summary>
///     How the drop zone of a <see cref="MokaFileUpload" /> looks while files are dragged over it. Applies to the
///     <see cref="MokaFileUploadLook.Standard" /> and <see cref="MokaFileUploadLook.CardList" /> zones; the other looks light
///     up their own way.
/// </summary>
public enum MokaFileUploadDragLook
{
	/// <summary>The border turns accent and the zone takes the accent tint, as on hover. The default.</summary>
	Standard,

	/// <summary>The border goes solid with a soft accent ring and glow round it. The inside stays light.</summary>
	Glow,

	/// <summary>The zone fills with the accent tint and swaps its words for one large drop icon and what the drop will do.</summary>
	Tint,

	/// <summary>The dashed border moves round the zone. It stands still when the system asks for less motion.</summary>
	Marching,

	/// <summary>The zone rises a little with a shadow, and the words change to say a drop uploads.</summary>
	Lift
}
