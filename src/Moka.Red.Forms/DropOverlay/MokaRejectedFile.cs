using Microsoft.AspNetCore.Components.Forms;

namespace Moka.Red.Forms.DropOverlay;

/// <summary>A dropped file that didn't pass a check, and which one.</summary>
/// <param name="File">The file.</param>
/// <param name="Reason">The check it failed.</param>
public sealed record MokaRejectedFile(IBrowserFile File, MokaFileRejection Reason);

/// <summary>Why a dropped file was turned away.</summary>
public enum MokaFileRejection
{
	/// <summary>Its type isn't in the accepted list.</summary>
	Type,

	/// <summary>It is larger than the size limit.</summary>
	Size,

	/// <summary>The drop brought more files than the limit, or several where one was wanted.</summary>
	Count
}
