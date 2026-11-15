using System.Text.RegularExpressions;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.SegmentedControl;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// A small segmented control was 36 px beside 32 px small fields, selects and buttons, so a toolbar of small controls
// didn't line up. MatchControlHeight gives it the fields' height; today's height stays the default.
public partial class MokaSegmentedControlHeightTests : BunitContext
{
	private const string Css = "MokaSegmentedControl.razor.css";

	private IRenderedComponent<MokaSegmentedControl> Control(bool match, MokaSize size = MokaSize.Sm) =>
		Render<MokaSegmentedControl>(p => p
			.Add(x => x.Size, size)
			.Add(x => x.MatchControlHeight, match)
			.Add(x => x.Value, "a")
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "a").Add(x => x.Text, "Day"))
			.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "b").Add(x => x.Text, "Week")));

	[Fact]
	public void TheDefault_KeepsTodaysClasses()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Control(false);

		Assert.Equal("moka-segmented moka-segmented--sm", cut.Find(".moka-segmented").ClassName);
	}

	[Fact]
	public void MatchControlHeight_IsAClassOnTheTrack()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Control(true);

		Assert.Equal("moka-segmented moka-segmented--sm moka-segmented--control-height", cut.Find(".moka-segmented").ClassName);
	}

	[Theory]
	[InlineData(".moka-gate .moka-segmented--control-height", "var(--moka-control-height)")]
	[InlineData(".moka-gate .moka-segmented--control-height.moka-segmented--xs", "var(--moka-control-height-xs)")]
	[InlineData(".moka-gate .moka-segmented--control-height.moka-segmented--sm", "var(--moka-control-height-sm)")]
	[InlineData(".moka-gate .moka-segmented--control-height.moka-segmented--lg", "var(--moka-control-height-lg)")]
	public void EachSize_TakesTheHeightAFieldOfThatSizeHas(string selector, string height) =>
		Assert.Equal(height, ScopedCss.Declarations(Css, selector)["--_segmented-height"]);

	// MokaTheme.WithControlHeights(Matched) sets these variables on the provider's root; elsewhere they are invalid.
	[Theory]
	[InlineData(".moka-gate .moka-segmented", "var(--moka-matched-height)")]
	[InlineData(".moka-gate .moka-segmented--xs", "var(--moka-matched-height-xs)")]
	[InlineData(".moka-gate .moka-segmented--sm", "var(--moka-matched-height-sm)")]
	[InlineData(".moka-gate .moka-segmented--lg", "var(--moka-matched-height-lg)")]
	public void UnderMatchedControlHeights_EachSize_TakesAFieldsHeight(string selector, string height) =>
		Assert.Equal(height, ScopedCss.Declarations(Css, selector)["--_segmented-height"]);

	[Fact]
	public void TheTrack_IsThatTall_AndTheSegmentsFillWhatItsPaddingAndBorderLeave()
	{
		IReadOnlyDictionary<string, string> track = ScopedCss.Declarations(Css, ".moka-gate .moka-segmented");

		Assert.Equal("var(--_segmented-height, auto)", track["min-height"]);
		Assert.Equal("calc(var(--_segmented-height) - 2 * (var(--moka-spacing-xs) + var(--moka-border-width)))", track["--_segment-height"]);
		Assert.Equal("var(--_segment-height, var(--moka-segment-height))",
			ScopedCss.Declarations(Css, ".moka-gate .moka-segmented ::deep .moka-segment")["min-height"]);
	}

	// Without a height of its own the variables are invalid, so the fallbacks keep today's segment heights.
	[Fact]
	public void SmallSegments_KeepTodaysHeight_WithoutOne() =>
		Assert.Equal("var(--_segment-height, calc(var(--moka-segment-height) - var(--moka-spacing-md)))",
			ScopedCss.Declarations(Css, ".moka-gate .moka-segmented--sm ::deep .moka-segment")["min-height"]);

	// The fields read these steps; the same text keeps the two in step.
	[Fact]
	public void TheTrack_UsesTheStepsTheGateTrackAlreadyPadsWith()
	{
		IReadOnlyDictionary<string, string> track = ScopedCss.Declarations(Css, ".moka-gate .moka-segmented");

		Assert.Equal("var(--moka-spacing-xs)", track["padding"]);
	}

	// The original design has no shared control height, so the option reaches only the new design.
	[Fact]
	public void EveryRuleForIt_IsUnderTheGateClass()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css)).ReplaceLineEndings("\n");

		List<string> heads = [.. RuleHead().Matches(css).Select(m => m.Groups[1].Value.Trim()).Where(h => h.Contains("--control-height", StringComparison.Ordinal) && !h.StartsWith("--", StringComparison.Ordinal))];

		Assert.NotEmpty(heads);
		Assert.All(heads, head => Assert.StartsWith(".moka-gate ", head, StringComparison.Ordinal));
	}

	[GeneratedRegex(@"([^{};/]+)\{")]
	private static partial Regex RuleHead();
}
