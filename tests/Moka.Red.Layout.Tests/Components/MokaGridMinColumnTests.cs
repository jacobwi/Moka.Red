using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Layout;
using Moka.Red.Layout.Grid;

namespace Moka.Red.Layout.Tests.Components;

public class MokaGridMinColumnTests : BunitContext
{
	private const string AutoFit300 = "repeat(auto-fit, minmax(min(100%, 300px), 1fr))";

	private static readonly IReadOnlyList<MokaBreakpoint> WideColumns =
	[
		new() { MinWidth = "1280px", Columns = 4 }
	];

	private string? GridStyle(Action<ComponentParameterCollectionBuilder<MokaGrid>> parameters) =>
		Render(parameters).Find(".moka-grid").GetAttribute("style");

	[Fact]
	public void Default_KeepsItsStyle()
	{
		Assert.Equal("display: grid; grid-template-columns: repeat(1, 1fr)", GridStyle(_ => { }));
	}

	[Fact]
	public void MinColumn_WritesTheAutoFitTemplate()
	{
		string? style = GridStyle(p => p.Add(x => x.MinColumn, "300px"));

		Assert.Equal($"display: grid; grid-template-columns: {AutoFit300}", style);
	}

	[Theory]
	[InlineData("var(--x)")]
	[InlineData("var(--x, 300px)")]
	public void MinColumn_TakesAVariable(string value)
	{
		string? style = GridStyle(p => p.Add(x => x.MinColumn, value));

		Assert.Contains($"repeat(auto-fit, minmax(min(100%, {value}), 1fr))", style, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("1px;color:red")]
	[InlineData("var(--x);color:red")]
	public void MinColumn_IgnoresAValueThatCouldEndItsDeclaration(string value)
	{
		string? style = GridStyle(p => p.Add(x => x.MinColumn, value));

		Assert.DoesNotContain("auto-fit", style, StringComparison.Ordinal);
		Assert.DoesNotContain("color:red", style, StringComparison.Ordinal);
	}

	[Fact]
	public void MinColumn_WinsOverColumns()
	{
		string? style = GridStyle(p => p.Add(x => x.MinColumn, "300px").Add(x => x.Columns, 3));

		Assert.Equal($"display: grid; grid-template-columns: {AutoFit300}", style);
	}

	[Fact]
	public void MinColumn_WinsOverColumnsValue()
	{
		string? style = GridStyle(p => p.Add(x => x.MinColumn, "300px").Add(x => x.ColumnsValue, "1fr 2fr"));

		Assert.Equal($"display: grid; grid-template-columns: {AutoFit300}", style);
	}

	[Theory]
	[InlineData("20rem", "20rem")]
	[InlineData(" 300px ", "300px")]
	[InlineData("300", "300px")]
	public void MinColumn_AlwaysCarriesAUnit(string value, string expected)
	{
		string? style = GridStyle(p => p.Add(x => x.MinColumn, value));

		Assert.Contains($"minmax(min(100%, {expected}), 1fr)", style, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("")]
	[InlineData("wide")]
	[InlineData("300px; color: red")]
	[InlineData("calc(100% - 1rem)")]
	public void MinColumn_ThatIsNotALength_IsIgnored(string value)
	{
		string? style = GridStyle(p => p.Add(x => x.MinColumn, value).Add(x => x.Columns, 3));

		Assert.Equal("display: grid; grid-template-columns: repeat(3, 1fr)", style);
	}

	// The !important on these classes is what lets them take over from the inline template at their widths.
	[Fact]
	public void MinColumn_LeavesTheColumnsClassesInPlace()
	{
		IElement grid = Render<MokaGrid>(p => p
			.Add(x => x.MinColumn, "300px")
			.Add(x => x.ColumnsLg, 4)).Find(".moka-grid");

		Assert.Contains("moka-grid--lg-4", grid.ClassList);
	}

	[Fact]
	public void MinColumn_WithBreakpoints_StaysInline_AndTheBreakpointTakesOverAtItsWidth()
	{
		IRenderedComponent<MokaGrid> cut = Render<MokaGrid>(p => p
			.Add(x => x.MinColumn, "300px")
			.Add(x => x.Breakpoints, WideColumns));

		IElement grid = cut.Find(".moka-grid");
		string uniqueClass = grid.ClassList.Single(c => c.StartsWith("moka-grid-", StringComparison.Ordinal));

		Assert.Contains(AutoFit300, grid.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("@media(min-width:1280px){." + uniqueClass + "{grid-template-columns: repeat(4, 1fr) !important}}",
			cut.Find("style").TextContent);
	}

	[Fact]
	public void Breakpoints_WithoutMinColumn_KeepTheirStyles()
	{
		IRenderedComponent<MokaGrid> cut = Render<MokaGrid>(p => p.Add(x => x.Breakpoints, WideColumns));

		Assert.Equal("display: grid; grid-template-columns: repeat(1, 1fr)", cut.Find(".moka-grid").GetAttribute("style"));
		Assert.StartsWith("@media(min-width:1280px)", cut.Find("style").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void InvalidMinColumn_WithBreakpoints_AddsNothingToTheStyleElement()
	{
		IRenderedComponent<MokaGrid> cut = Render<MokaGrid>(p => p
			.Add(x => x.MinColumn, "wide")
			.Add(x => x.Breakpoints, WideColumns));

		Assert.StartsWith("@media(min-width:1280px)", cut.Find("style").TextContent, StringComparison.Ordinal);
	}
}
