namespace Moka.Red.Primitives.DataList;

/// <summary>
///     One of several values a <see cref="MokaDataListItem" /> shows through <see cref="MokaDataListItem.Values" />, each
///     on its own line with its own copy button when the item is <see cref="MokaDataListItem.Copyable" />.
/// </summary>
/// <param name="Text">The value as it shows, and what its copy button copies unless <see cref="CopyText" /> says otherwise.</param>
/// <param name="Note">
///     A quiet word after the value in the text font, such as "until 14:05" after an address. It isn't copied.
/// </param>
public sealed record MokaDataListValue(string Text, string? Note = null)
{
	/// <summary>What the copy button copies when it differs from <see cref="Text" />, such as a full key shown shortened.</summary>
	public string? CopyText { get; init; }

	/// <summary>
	///     Names this value's copy button and its tooltip. Null is the list's name for the item's button followed by the
	///     value: "Copy Addresses, 192.168.1.20".
	/// </summary>
	public string? CopyLabel { get; init; }

	internal string? TextToCopy(bool copyable) =>
		copyable && !string.IsNullOrEmpty(CopyText ?? Text) ? CopyText ?? Text : null;
}
