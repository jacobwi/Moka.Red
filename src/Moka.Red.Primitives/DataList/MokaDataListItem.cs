using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;

namespace Moka.Red.Primitives.DataList;

/// <summary>
///     Represents a single term/description pair in a <see cref="MokaDataList" />.
/// </summary>
/// <param name="Label">The term or label text (renders as &lt;dt&gt;).</param>
/// <param name="Value">The description text (renders as &lt;dd&gt;). Used when <see cref="ValueContent" /> is null.</param>
/// <param name="ValueContent">Optional render fragment for rich description content.</param>
public record MokaDataListItem(
	string Label,
	string? Value = null,
	RenderFragment? ValueContent = null)
{
	/// <summary>An icon before the term. Decorative: the term still names the value.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>
	///     A line under the term saying what it is for. It carries the <c>moka-about</c> class, so a theme with
	///     descriptions turned off hides it, unless <see cref="AlwaysShowHelp" /> is on.
	/// </summary>
	public string? Help { get; init; }

	/// <summary>
	///     Shows <see cref="Help" /> whatever the theme's descriptions say, for a line that says what is so rather than
	///     what the term is for, such as "Windows calls it the thumbprint". Default false.
	/// </summary>
	public bool AlwaysShowHelp { get; init; }

	/// <summary>
	///     Several values, one per line, in place of <see cref="Value" /> and <see cref="ValueContent" />, such as a
	///     device's addresses. With <see cref="Copyable" /> on, each value gets its own copy button in the list's
	///     <see cref="MokaDataList.CopyStyle" />, copying its own text, and <see cref="CopyText" /> doesn't apply. An empty
	///     list counts as no value.
	/// </summary>
	public IReadOnlyList<MokaDataListValue>? Values { get; init; }

	/// <summary>
	///     Classes on the item's row, the <c>div</c> holding its term and value, after the list's own. An app's stylesheet
	///     reaches the value as <c>.your-class &gt; dd</c>.
	/// </summary>
	public string? Class { get; init; }

	/// <summary>Draws the value in the mono font with even-width digits, for addresses, keys and IDs.</summary>
	public bool Mono { get; init; }

	/// <summary>
	///     Lets the value be copied, as <see cref="MokaDataList.CopyStyle" /> draws it: by default a quiet copy button after
	///     the value, named "Copy" and the <see cref="Label" />. It copies <see cref="CopyText" />, else
	///     <see cref="Value" />, and shows nothing when both are empty.
	/// </summary>
	public bool Copyable { get; init; }

	/// <summary>What the copy button copies when it differs from what shows, such as a full key shown shortened.</summary>
	public string? CopyText { get; init; }

	/// <summary>Controls at the row's end, after the value: an edit or reveal button.</summary>
	public RenderFragment? EndContent { get; init; }

	/// <summary>
	///     Takes a whole row when the list is laid out in <see cref="MokaDataList.Columns" /> or as
	///     <see cref="MokaDataListLook.FactTiles" />, for a long value such as a key. A pair written as child content gets
	///     the same with the <c>moka-data-list-entry--wide</c> class on its <c>div</c>.
	/// </summary>
	public bool Wide { get; init; }

	internal string? TextToCopy => Copyable && Values is null && !string.IsNullOrEmpty(CopyText ?? Value) ? CopyText ?? Value : null;

	internal bool HasSeveral => Values is { Count: > 0 };

	internal bool CopiesSeveral => Copyable && Values is not null && Values.Any(value => value.TextToCopy(true) is not null);
}
