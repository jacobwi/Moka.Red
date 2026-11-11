using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;

namespace Moka.Red.ContextMenu;

/// <summary>What a <see cref="MokaRadialMenu" /> holds: the name of what was right-clicked, and its buttons.</summary>
/// <param name="Title">The name of what the menu acts on. The hub shows it, and it names the menu for screen readers.</param>
/// <param name="Items">The buttons, clockwise from the top. Digits 1 to 9 pick the first nine.</param>
/// <param name="Sub">The word under the title while no button is highlighted, such as what the thing is or its state.</param>
/// <param name="Icon">An icon over the title, in <paramref name="Color" />.</param>
/// <param name="Color">The color of the hub's edge, glow and icon.</param>
/// <param name="OnPick">Runs with what was picked, once the menu has closed.</param>
public sealed record MokaRadialMenuSpec(
	string Title,
	IReadOnlyList<MokaRadialItem> Items,
	string? Sub = null,
	MokaIconDefinition? Icon = null,
	MokaColor Color = MokaColor.Primary,
	Func<MokaRadialPick, Task>? OnPick = null)
{
	/// <summary>
	///     How the buttons sit around the target. Null leaves it to the menu: <see cref="MokaRadialMenu.Shape" /> wins
	///     over this, and without either it is a <see cref="MokaRadialShape.Ring" />.
	/// </summary>
	public MokaRadialShape? Shape { get; init; }

	/// <summary>
	///     How the buttons are drawn. Null leaves it to the menu: <see cref="MokaRadialMenu.Look" /> wins over this, and
	///     without either the menu follows the theme's <see cref="MokaTheme.Popups" />.
	/// </summary>
	public MokaRadialLook? Look { get; init; }

	/// <summary>
	///     How a button's choices show. Null leaves it to the menu: <see cref="MokaRadialMenu.ChoiceStyle" /> wins over
	///     this, and without either they open in a <see cref="MokaRadialChoiceStyle.List" />.
	/// </summary>
	public MokaRadialChoiceStyle? ChoiceStyle { get; init; }

	/// <summary>
	///     Whether a <see cref="MokaRadialShape.Ring" /> turns into a <see cref="MokaRadialShape.HalfRing" /> near the
	///     bottom of the window and a <see cref="MokaRadialShape.CornerFan" /> near a corner. Null leaves it to the menu:
	///     <see cref="MokaRadialMenu.AdaptToEdges" /> wins over this, and without either it stays a ring.
	/// </summary>
	public bool? AdaptToEdges { get; init; }

	/// <summary>
	///     The hub's word for going back while <see cref="MokaRadialChoiceStyle.Replace" /> shows a button's choices.
	///     <see cref="MokaRadialMenu.BackText" /> wins over this, and without either it says "Back".
	/// </summary>
	public string? BackText { get; init; }
}
