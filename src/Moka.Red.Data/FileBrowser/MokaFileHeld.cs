namespace Moka.Red.Data.FileBrowser;

/// <summary>What a <see cref="MokaFileBrowser{TItem}" /> is holding for the next paste.</summary>
/// <typeparam name="TItem">The item type of the browser.</typeparam>
/// <param name="Items">The items held.</param>
/// <param name="IsCut">Whether a paste moves them, rather than copying them.</param>
public sealed record MokaFileHeld<TItem>(IReadOnlyList<TItem> Items, bool IsCut);
