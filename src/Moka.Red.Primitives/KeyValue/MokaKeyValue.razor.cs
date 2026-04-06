using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.KeyValue;

/// <summary>
///     Displays a key-value pair as a styled definition item.
///     Supports horizontal (side-by-side) and vertical (stacked) orientations,
///     optional copy-to-clipboard, and rich content via <see cref="ChildContent" />.
/// </summary>
public partial class MokaKeyValue : MokaComponentBase
{
	/// <summary>The label (key) text.</summary>
	[Parameter]
	[EditorRequired]
	public string Label { get; set; } = string.Empty;

	/// <summary>The value text. Ignored when <see cref="ChildContent" /> is provided.</summary>
	[Parameter]
	public string? Value { get; set; }

	/// <summary>Optional rich content that replaces <see cref="Value" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Layout orientation. Row = side by side, Column = stacked. Defaults to Row.</summary>
	[Parameter]
	public MokaDirection Orientation { get; set; } = MokaDirection.Row;

	/// <summary>Fixed width for the label. Useful for alignment in lists of key-value pairs.</summary>
	[Parameter]
	public string? LabelWidth { get; set; }

	/// <summary>Whether to show a copy button for the value. Defaults to false.</summary>
	[Parameter]
	public bool Copyable { get; set; }

	/// <summary>Whether to truncate the value with ellipsis. Defaults to false.</summary>
	[Parameter]
	public bool Truncate { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-kv";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-kv--row", Orientation == MokaDirection.Row)
		.AddClass("moka-kv--column", Orientation == MokaDirection.Column)
		.AddClass("moka-kv--truncate", Truncate)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	private string? LabelStyle => new StyleBuilder()
		.AddStyle("width", LabelWidth, !string.IsNullOrEmpty(LabelWidth))
		.AddStyle("min-width", LabelWidth, !string.IsNullOrEmpty(LabelWidth))
		.Build();

	private bool HasContent => ChildContent is not null || !string.IsNullOrEmpty(Value);
}
