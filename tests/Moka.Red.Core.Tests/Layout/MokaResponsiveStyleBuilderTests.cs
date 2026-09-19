using Moka.Red.Core.Layout;

namespace Moka.Red.Core.Tests.Layout;

public class MokaResponsiveStyleBuilderTests
{
	[Fact]
	public void BuildGridStyles_OrdersBreakpointsByNumericWidth()
	{
		// Ordering the raw MinWidth strings put "1024px" before "768px", so the wider
		// breakpoint lost the cascade.
		MokaBreakpoint[] breakpoints =
		[
			new() { MinWidth = "1024px", Columns = 4 },
			new() { MinWidth = "768px", Columns = 2 },
			new() { MinWidth = "480px", Columns = 1 }
		];

		string? css = MokaResponsiveStyleBuilder.BuildGridStyles("moka-grid-1", breakpoints);

		Assert.NotNull(css);
		int small = css.IndexOf("480px", StringComparison.Ordinal);
		int medium = css.IndexOf("768px", StringComparison.Ordinal);
		int large = css.IndexOf("1024px", StringComparison.Ordinal);
		Assert.True(small < medium, "480px must be emitted before 768px");
		Assert.True(medium < large, "768px must be emitted before 1024px");
	}

	[Fact]
	public void BuildGridStyles_NormalisesRemAgainstPx()
	{
		// 48rem is 768px at the 16px browser default, so it must sort between 480px and 1024px.
		MokaBreakpoint[] breakpoints =
		[
			new() { MinWidth = "1024px", Columns = 4 },
			new() { MinWidth = "48rem", Columns = 2 },
			new() { MinWidth = "480px", Columns = 1 }
		];

		string? css = MokaResponsiveStyleBuilder.BuildGridStyles("moka-grid-2", breakpoints);

		Assert.NotNull(css);
		int small = css.IndexOf("480px", StringComparison.Ordinal);
		int medium = css.IndexOf("48rem", StringComparison.Ordinal);
		int large = css.IndexOf("1024px", StringComparison.Ordinal);
		Assert.True(small < medium && medium < large);
	}

	[Fact]
	public void BuildFlexboxStyles_OrdersBreakpointsByNumericWidth()
	{
		MokaBreakpoint[] breakpoints =
		[
			new() { MinWidth = "1200px", Wrap = false },
			new() { MinWidth = "600px", Wrap = true }
		];

		string? css = MokaResponsiveStyleBuilder.BuildFlexboxStyles("moka-flex-1", breakpoints);

		Assert.NotNull(css);
		Assert.True(css.IndexOf("600px", StringComparison.Ordinal) < css.IndexOf("1200px", StringComparison.Ordinal));
	}

	[Fact]
	public void BuildGridStyles_ReturnsNullForEmptyBreakpoints()
	{
		string? css = MokaResponsiveStyleBuilder.BuildGridStyles("moka-grid-3", []);

		Assert.Null(css);
	}

	[Fact]
	public void BuildGridStyles_SortsUnparsableWidthsLast()
	{
		MokaBreakpoint[] breakpoints =
		[
			new() { MinWidth = "calc(100% - 2rem)", Columns = 3 },
			new() { MinWidth = "600px", Columns = 2 }
		];

		string? css = MokaResponsiveStyleBuilder.BuildGridStyles("moka-grid-4", breakpoints);

		Assert.NotNull(css);
		Assert.True(css.IndexOf("600px", StringComparison.Ordinal) < css.IndexOf("calc(", StringComparison.Ordinal));
	}

	// row-gap works on a flex container, but the flexbox builder dropped it.
	[Fact]
	public void BuildFlexboxStyles_EmitsRowGap()
	{
		MokaBreakpoint[] breakpoints = [new() { MinWidth = "768px", RowGapValue = "12px" }];

		string? css = MokaResponsiveStyleBuilder.BuildFlexboxStyles("moka-flex-1", breakpoints);

		Assert.NotNull(css);
		Assert.Contains("row-gap: 12px", css, StringComparison.Ordinal);
	}

	// The output lands in a <style> element. A value that closes the block or the element must not
	// get through.
	[Fact]
	public void UnsafeValues_AreDropped()
	{
		MokaBreakpoint[] breakpoints =
		[
			new() { MinWidth = "768px", Columns = 2, GapValue = "1px} body{display:none" },
			new() { MinWidth = "1px}</style><script>", Columns = 3 }
		];

		string? css = MokaResponsiveStyleBuilder.BuildGridStyles("moka-grid-9", breakpoints);

		Assert.NotNull(css);
		Assert.Contains("repeat(2, 1fr)", css, StringComparison.Ordinal);
		Assert.DoesNotContain("body", css, StringComparison.Ordinal);
		Assert.DoesNotContain("<", css, StringComparison.Ordinal);
		Assert.DoesNotContain("repeat(3, 1fr)", css, StringComparison.Ordinal);
	}

	// A bracket or string left open did not escape the rule, but it swallowed every rule after it.
	[Fact]
	public void AValueLeftOpen_IsDropped_AndTheWiderBreakpointsSurvive()
	{
		MokaBreakpoint[] breakpoints =
		[
			new() { MinWidth = "480px", Columns = 1, ColumnsValue = "repeat(2, 1fr" },
			new() { MinWidth = "768px", Columns = 2, GapValue = "'1rem" },
			new() { MinWidth = "1024px", Columns = 4 }
		];

		string? css = MokaResponsiveStyleBuilder.BuildGridStyles("moka-grid-10", breakpoints);

		Assert.NotNull(css);
		Assert.DoesNotContain("repeat(2, 1fr;", css, StringComparison.Ordinal);
		Assert.DoesNotContain("'1rem", css, StringComparison.Ordinal);
		Assert.Contains("@media(min-width:1024px){.moka-grid-10{grid-template-columns: repeat(4, 1fr)}}", css,
			StringComparison.Ordinal);
		Assert.Equal(css.Count(c => c == '('), css.Count(c => c == ')'));
	}

	[Fact]
	public void ASelectorThatIsNotAClassName_Throws()
	{
		MokaBreakpoint[] breakpoints = [new() { MinWidth = "768px", Columns = 2 }];

		Assert.Throws<ArgumentException>(() => MokaResponsiveStyleBuilder.BuildGridStyles("x{}", breakpoints));
	}
}
