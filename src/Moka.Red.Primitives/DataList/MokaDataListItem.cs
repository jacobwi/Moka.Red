using Microsoft.AspNetCore.Components;

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
	RenderFragment? ValueContent = null);
