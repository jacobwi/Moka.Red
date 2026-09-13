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
}
