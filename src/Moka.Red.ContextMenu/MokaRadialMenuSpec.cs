using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;

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
	Func<MokaRadialPick, Task>? OnPick = null);
