namespace Moka.Red.Layout.BentoGrid;

/// <summary>
///     One card on a <see cref="MokaBentoGrid" /> board. A board is a list of these in the order the person arranged
///     them, hidden cards included, so it can be stored as it is and handed back.
/// </summary>
/// <param name="Id">Names the card. Unique on its board: a repeated id is dropped.</param>
/// <param name="Size">How much of a row the card takes.</param>
/// <param name="Hidden">Whether the card waits in the tray instead of showing on the board.</param>
public sealed record MokaBoardCard(string Id, MokaBoardSize Size = MokaBoardSize.Wide, bool Hidden = false);
