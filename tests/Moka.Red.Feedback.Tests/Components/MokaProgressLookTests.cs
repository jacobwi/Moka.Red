using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.Progress;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public partial class MokaProgressLookTests : BunitContext
{
	private const string Stylesheet = "MokaProgress.razor.css";

	private static readonly IReadOnlyList<MokaProgressSegment> FourSegments =
	[
		new(46, MokaColor.Success),
		new(18, MokaColor.Warning),
		new(9, MokaColor.Error),
		new(12, MokaColor.Info)
	];

	private IRenderedComponent<MokaProgress> WithSegments(params MokaProgressSegment[] segments) =>
		Render<MokaProgress>(p => p.Add(x => x.Segments, segments));

	[Fact]
	public void Default_KeepsItsMarkup()
	{
		IRenderedComponent<MokaProgress> cut = Render<MokaProgress>(p => p.Add(x => x.Value, 40d));

		Assert.Equal(
			"""<div class="moka-progress moka-fill-width moka-progress--linear moka-progress--rounded" role="progressbar" """ +
			"""aria-valuenow="40" aria-valuemin="0" aria-valuemax="100"><div class="moka-progress-track">""" +
			"""<div class="moka-progress-bar" style="width: 40%"></div></div></div>""",
			ScopeAttribute().Replace(cut.Markup, ""));
	}

	[Fact]
	public void Thin_AddsItsClass()
	{
		IElement root = Render<MokaProgress>(p => p.Add(x => x.Thin, true)).Find(".moka-progress");

		Assert.Contains("moka-progress--thin", root.ClassList);
	}

	[Fact]
	public void Thin_DoesNothingOnACircle()
	{
		IElement root = Render<MokaProgress>(p => p
			.Add(x => x.Thin, true)
			.Add(x => x.ProgressType, MokaProgressType.Circular)).Find(".moka-progress");

		Assert.DoesNotContain("moka-progress--thin", root.ClassList);
	}

	[Fact]
	public void ThinRule_Is6PixelsAndRound()
	{
		IReadOnlyDictionary<string, string> track =
			ScopedCss.Declarations(Stylesheet, ".moka-progress--thin .moka-progress-track");

		Assert.Equal("0.375rem", track["height"]);
		Assert.Equal("var(--moka-color-outline-variant)", track["background"]);
		Assert.Equal("var(--moka-radius-full)", track["border-radius"]);
	}

	[Fact]
	public void SegmentedRule_LaysThemSideBySideWithAGap()
	{
		IReadOnlyDictionary<string, string> track =
			ScopedCss.Declarations(Stylesheet, ".moka-progress--segmented .moka-progress-track");

		Assert.Equal("flex", track["display"]);
		Assert.Equal("var(--moka-spacing-xxs)", track["gap"]);
	}

	[Fact]
	public void Segments_DrawOneBlockEach_WithTheirWidthAndColor()
	{
		IRenderedComponent<MokaProgress> cut = Render<MokaProgress>(p => p.Add(x => x.Segments, FourSegments));

		IReadOnlyList<IElement> segments = cut.FindAll(".moka-progress-segment");

		Assert.Equal(
			[
				"width: 46%; background: var(--moka-color-success)",
				"width: 18%; background: var(--moka-color-warning)",
				"width: 9%; background: var(--moka-color-error)",
				"width: 12%; background: var(--moka-color-info)"
			],
			segments.Select(segment => segment.GetAttribute("style")));
		Assert.Contains("moka-progress--segmented", cut.Find(".moka-progress").ClassList);
	}

	[Fact]
	public void Segments_WinOverValue()
	{
		IRenderedComponent<MokaProgress> cut = Render<MokaProgress>(p => p
			.Add(x => x.Value, 5d)
			.Add(x => x.Segments, FourSegments));

		Assert.Empty(cut.FindAll(".moka-progress-bar"));
		Assert.Equal("85", cut.Find(".moka-progress").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void Segments_AreNotIndeterminate()
	{
		IElement root = Render<MokaProgress>(p => p.Add(x => x.Segments, FourSegments)).Find(".moka-progress");

		Assert.DoesNotContain("moka-progress--indeterminate", root.ClassList);
	}

	[Fact]
	public void ShowValue_ShowsTheirSum()
	{
		IRenderedComponent<MokaProgress> cut = Render<MokaProgress>(p => p
			.Add(x => x.Segments, FourSegments)
			.Add(x => x.ShowValue, true));

		Assert.Equal("85%", cut.Find(".moka-progress-value").TextContent);
	}

	[Fact]
	public void Segments_UseTheInvariantCulture()
	{
		CultureInfo original = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		try
		{
			IRenderedComponent<MokaProgress> cut = WithSegments(new MokaProgressSegment(12.5, MokaColor.Primary));

			Assert.Equal("width: 12.5%; background: var(--moka-color-primary)",
				cut.Find(".moka-progress-segment").GetAttribute("style"));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[Fact]
	public void SegmentOutsideTheRange_IsBroughtBack()
	{
		IRenderedComponent<MokaProgress> cut = WithSegments(new MokaProgressSegment(250, MokaColor.Primary));

		Assert.StartsWith("width: 100%;", cut.Find(".moka-progress-segment").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("100", cut.Find(".moka-progress").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void SegmentsAddingUpPast100_ReportAtMost100()
	{
		IRenderedComponent<MokaProgress> cut = WithSegments(
			new MokaProgressSegment(70, MokaColor.Primary),
			new MokaProgressSegment(70, MokaColor.Error));

		Assert.Equal("100", cut.Find(".moka-progress").GetAttribute("aria-valuenow"));
	}

	// Each one drawn would add a gap with nothing beside it.
	[Theory]
	[InlineData(0d)]
	[InlineData(-5d)]
	[InlineData(double.NaN)]
	public void EmptySegments_AreNotDrawn(double value)
	{
		IRenderedComponent<MokaProgress> cut = WithSegments(
			new MokaProgressSegment(value, MokaColor.Primary),
			new MokaProgressSegment(30, MokaColor.Error));

		Assert.Single(cut.FindAll(".moka-progress-segment"));
		Assert.Equal("30", cut.Find(".moka-progress").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void AnEmptyList_IsAnEmptyBar_NotAnIndeterminateOne()
	{
		IRenderedComponent<MokaProgress> cut = WithSegments();

		Assert.Empty(cut.FindAll(".moka-progress-segment"));
		Assert.DoesNotContain("moka-progress--indeterminate", cut.Find(".moka-progress").ClassList);
		Assert.Equal("0", cut.Find(".moka-progress").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void Segments_AreIgnoredOnACircle()
	{
		IRenderedComponent<MokaProgress> cut = Render<MokaProgress>(p => p
			.Add(x => x.ProgressType, MokaProgressType.Circular)
			.Add(x => x.Value, 40d)
			.Add(x => x.Segments, FourSegments));

		Assert.Empty(cut.FindAll(".moka-progress-segment"));
		Assert.DoesNotContain("moka-progress--segmented", cut.Find(".moka-progress").ClassList);
		Assert.Equal("40", cut.Find(".moka-progress").GetAttribute("aria-valuenow"));
	}

	[GeneratedRegex(@" b-\w+(="""")?")]
	private static partial Regex ScopeAttribute();
}
