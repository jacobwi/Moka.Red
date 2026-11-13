using Microsoft.AspNetCore.Components;

namespace Moka.Red.ContextMenu;

/// <summary>The radial menu <see cref="IMokaContextMenuService" /> has open, for <see cref="MokaContextMenuHost" /> to draw.</summary>
/// <param name="Spec">What the menu holds.</param>
/// <param name="X">The centre's distance from the left of the viewport, in pixels.</param>
/// <param name="Y">The centre's distance from the top of the viewport, in pixels.</param>
/// <param name="OpenedFromKeyboard">Whether the keyboard opened it, so it starts on its first button.</param>
/// <param name="AnchorToFocus">Whether it centres on whatever has focus instead of on <paramref name="X" /> and <paramref name="Y" />.</param>
/// <param name="Flick">Whether a button was still held when it opened, so letting go over a button picks it.</param>
public sealed record MokaRadialMenuRequest(
	MokaRadialMenuSpec Spec,
	double X,
	double Y,
	bool OpenedFromKeyboard = false,
	bool AnchorToFocus = false,
	bool Flick = false)
{
	/// <summary>
	///     The component that redraws after <see cref="MokaRadialMenuSpec.OnPick" /> runs, as an
	///     <see cref="EventCallback" />'s receiver does. Null runs the pick from the menu.
	/// </summary>
	public IHandleEvent? Owner { get; init; }
}
