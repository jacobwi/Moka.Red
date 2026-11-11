using Microsoft.AspNetCore.Components.Web;

namespace Moka.Red.Core.Keys;

/// <summary>
///     The arguments of <c>@onmokakeydown</c>: a keydown with who it belongs to, worked out in the browser where the
///     press's target is known. A page-level shortcut handler ignores a press whose <see cref="Owner" /> isn't
///     <see cref="MokaKeyOwner.None" />, so typing in a field, Space on a button and every key inside an open dialog
///     are left alone.
/// </summary>
public sealed class MokaKeyEventArgs : KeyboardEventArgs
{
	/// <summary>What the press belongs to. A field or control wins over a modal around it.</summary>
	public MokaKeyOwner Owner { get; set; }

	/// <summary>Whether the press was inside an open modal, whatever else owns it.</summary>
	public bool InModal { get; set; }

	/// <summary>Whether something other than the page owns the press. Shortcut handlers on the page skip these.</summary>
	public bool Owned => Owner != MokaKeyOwner.None;
}
