namespace Moka.Red.Primitives.Kanban;

/// <summary>
///     Every word a <see cref="MokaKanbanBoard{TItem}" /> says to a screen reader while a card moves. The announcements
///     are templates the browser fills in as the card moves, so a move costs no call to .NET: <c>{item}</c> is the card's
///     <c>aria-label</c>, else its text, <c>{column}</c> the column's title, <c>{position}</c> the place in that column
///     counted from 1, and <c>{count}</c> how many places the column has.
/// </summary>
public sealed record MokaKanbanText
{
	/// <summary>The words this board uses unless it is given others.</summary>
	public static MokaKanbanText Default { get; } = new();

	/// <summary>How to move a card from the keyboard, read when a card takes focus.</summary>
	public string Instructions { get; init; } =
		"Press Space to pick the card up, the arrow keys to move it, Space to drop it and Escape to put it back.";

	/// <summary>Said when a card is picked up.</summary>
	public string PickedUp { get; init; } = "Picked up {item}. {column}, position {position} of {count}.";

	/// <summary>Said each time a picked-up card moves.</summary>
	public string Moving { get; init; } = "{column}, position {position} of {count}.";

	/// <summary>Said when a card is dropped.</summary>
	public string Dropped { get; init; } = "Dropped {item} in {column}, position {position} of {count}.";

	/// <summary>Said when a move is cancelled and the card stays where it was.</summary>
	public string Cancelled { get; init; } = "{item} stays in {column}, position {position}.";
}
