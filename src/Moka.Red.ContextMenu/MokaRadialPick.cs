namespace Moka.Red.ContextMenu;

/// <summary>What was picked in a <see cref="MokaRadialMenu" />.</summary>
/// <param name="Item">The button.</param>
/// <param name="Choice">The choice in the button's list, or null when the button itself was picked.</param>
public sealed record MokaRadialPick(MokaRadialItem Item, MokaRadialChoice? Choice = null)
{
	/// <summary>The button's id, or <c>{item}:{choice}</c> for a choice in its list.</summary>
	public string Answer => Choice is null ? Item.Id : $"{Item.Id}:{Choice.Id}";
}
