using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaSkeletonGateTests
{
	private const string Stylesheet = "MokaSkeleton.razor.css";

	private const string GateLine = ".moka-gate .moka-skeleton--shimmer .moka-skeleton-line";

	[Theory]
	[InlineData(".moka-gate .moka-skeleton--shimmer .moka-skeleton-line")]
	[InlineData(".moka-gate .moka-skeleton--shimmer .moka-skeleton-circle")]
	[InlineData(".moka-gate .moka-skeleton--shimmer .moka-skeleton-rect")]
	[InlineData(".moka-gate .moka-skeleton--shimmer .moka-skeleton-card-image")]
	public void GateShimmer_SweepsAWideGradient(string selector)
	{
		IReadOnlyDictionary<string, string> shimmer = ScopedCss.Declarations(Stylesheet, selector);

		Assert.Contains("var(--moka-color-surface-variant) 0%", shimmer["background"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-color-surface-hover) 40%", shimmer["background"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-color-surface-variant) 80%", shimmer["background"], StringComparison.Ordinal);
		Assert.Equal("300% 100%", shimmer["background-size"]);
	}

	[Fact]
	public void GateShimmer_Takes1Point4Seconds()
	{
		Assert.Equal("moka-skeleton-sweep 1.4s ease-in-out infinite", ScopedCss.Values(Stylesheet, GateLine, "animation")[0]);
	}

	// With a 300% wide gradient, -150% is exactly one repeat. The shared moka-shimmer would jump at each loop.
	[Fact]
	public void GateShimmer_LoopsWithoutAJump()
	{
		Assert.Equal("-150% 0", ScopedCss.Declarations(Stylesheet, "to")["background-position"]);
	}

	[Fact]
	public void GateShimmer_StopsForReducedMotion()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet));

		Assert.Equal("none", ScopedCss.Declarations(Stylesheet, GateLine)["animation"]);
		Assert.Contains("@media (prefers-reduced-motion: reduce)", css, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(".moka-gate .moka-skeleton-rect")]
	[InlineData(".moka-gate .moka-skeleton-card-image")]
	public void GateShapes_HaveTheSmallCorner(string selector)
	{
		Assert.Equal("var(--moka-radius-sm)", ScopedCss.Declarations(Stylesheet, selector)["border-radius"]);
	}

	[Fact]
	public void TheCircle_StaysRound()
	{
		Assert.Empty(ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-skeleton-circle"));
	}

	[Fact]
	public void TheMatrixShimmer_IsUntouched()
	{
		IReadOnlyDictionary<string, string> matrix =
			ScopedCss.Declarations(Stylesheet, ".moka-skeleton--shimmer .moka-skeleton-line");

		Assert.Equal("200% 100%", matrix["background-size"]);
		Assert.Equal("moka-shimmer 1.5s ease-in-out infinite", matrix["animation"]);
	}
}
