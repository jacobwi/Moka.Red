using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.BentoGrid;

/// <summary>
///     An asymmetric bento-box grid layout (like Apple marketing pages).
///     Children are placed using CSS grid with configurable column/row spans.
///     Use <see cref="MokaBentoItem" /> for individual cells with span control.
/// </summary>
public partial class MokaBentoGrid : MokaComponentBase
{
	/// <summary>Grid items (use <see cref="MokaBentoItem" /> children).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Number of columns. Default 4.</summary>
	[Parameter]
	public int Columns { get; set; } = 4;

	/// <summary>Gap between cells. Default Md.</summary>
	[Parameter]
	public MokaSpacingScale Gap { get; set; } = MokaSpacingScale.Md;

	/// <summary>Custom gap override.</summary>
	[Parameter]
	public string? GapValue { get; set; }

	/// <summary>Auto row height. Default "auto".</summary>
	[Parameter]
	public string RowHeight { get; set; } = "auto";

	/// <summary>Minimum row height. Default "120px".</summary>
	[Parameter]
	public string MinRowHeight { get; set; } = "120px";

	/// <inheritdoc />
	protected override string RootClass => "moka-bento-grid";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private string ResolvedGap => GapValue ?? MokaEnumHelpers.ToCssValue(Gap);

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("grid-template-columns", $"repeat({Columns}, 1fr)")
		.AddStyle("grid-auto-rows", RowHeight == "auto" ? $"minmax({MinRowHeight}, auto)" : RowHeight)
		.AddStyle("gap", ResolvedGap)
		.AddStyle(Style)
		.Build();
}
