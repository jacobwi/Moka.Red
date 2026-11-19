using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.Progress;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// Color was inherited from the base but never read, so every bar and ring drew in the primary color.
public partial class MokaProgressColorTests : BunitContext
{
	private const string Stylesheet = "MokaProgress.razor.css";
	private const string ReadsTheColor = "var(--_progress-color, var(--moka-color-primary))";

	private IElement Root(MokaProgressType type, MokaColor? color) =>
		Render<MokaProgress>(p => p
			.Add(x => x.ProgressType, type)
			.Add(x => x.Value, 40d)
			.Add(x => x.Color, color)).Find(".moka-progress");

	[Theory]
	[InlineData(MokaColor.Success, "var(--moka-color-success)")]
	[InlineData(MokaColor.Error, "var(--moka-color-error)")]
	[InlineData(MokaColor.Primary, "var(--moka-color-primary)")]
	[InlineData(MokaColor.Surface, "var(--moka-color-on-surface)")]
	public void Color_IsWrittenOnTheLinearRoot(MokaColor color, string expected)
	{
		IRenderedComponent<MokaProgress> cut = Render<MokaProgress>(p => p
			.Add(x => x.Value, 40d)
			.Add(x => x.Color, color));

		Assert.Equal($"--_progress-color: {expected}", cut.Find(".moka-progress").GetAttribute("style"));
		Assert.Equal("width: 40%", cut.Find(".moka-progress-bar").GetAttribute("style"));
	}

	[Fact]
	public void Color_IsWrittenOnTheCircularRoot() =>
		Assert.Equal("--_progress-color: var(--moka-color-warning)",
			Root(MokaProgressType.Circular, MokaColor.Warning).GetAttribute("style"));

	[Theory]
	[InlineData(MokaProgressType.Linear)]
	[InlineData(MokaProgressType.Circular)]
	public void NoColor_WritesNoStyle(MokaProgressType type) =>
		Assert.False(Root(type, null).HasAttribute("style"));

	[Fact]
	public void TheStyleParameter_StillComesLast()
	{
		IElement root = Render<MokaProgress>(p => p
			.Add(x => x.Value, 40d)
			.Add(x => x.Color, MokaColor.Info)
			.Add(x => x.Style, "--_progress-color: rebeccapurple")).Find(".moka-progress");

		Assert.Equal("--_progress-color: var(--moka-color-info); --_progress-color: rebeccapurple", root.GetAttribute("style"));
	}

	[Fact]
	public void ACircleWithoutAColor_KeepsItsMarkup()
	{
		IRenderedComponent<MokaProgress> cut = Render<MokaProgress>(p => p
			.Add(x => x.ProgressType, MokaProgressType.Circular)
			.Add(x => x.Value, 40d));

		Assert.Equal(
			"""<div class="moka-progress moka-progress--circular" role="progressbar" aria-valuenow="40" aria-valuemin="0" aria-valuemax="100">""" +
			"""<svg class="moka-progress-circle" width="44" height="44" viewBox="0 0 44 44">""" +
			"""<circle class="moka-progress-circle-track" cx="22" cy="22" r="18" fill="none" stroke-width="4"></circle>""" +
			"""<circle class="moka-progress-circle-bar" cx="22" cy="22" r="18" fill="none" stroke-width="4" stroke-linecap="round" """ +
			"""stroke-dasharray="113.10" stroke-dashoffset="67.86"></circle></svg></div>""",
			BetweenTags().Replace(ScopeAttribute().Replace(cut.Markup, ""), "><"));
	}

	// The segments name their own colors, which win over the bar's.
	[Fact]
	public void Segments_KeepTheirOwnColors()
	{
		IRenderedComponent<MokaProgress> cut = Render<MokaProgress>(p => p
			.Add(x => x.Color, MokaColor.Error)
			.Add(x => x.Segments, [new MokaProgressSegment(30, MokaColor.Success), new MokaProgressSegment(20, MokaColor.Info)]));

		Assert.Equal(
			["width: 30%; background: var(--moka-color-success)", "width: 20%; background: var(--moka-color-info)"],
			cut.FindAll(".moka-progress-segment").Select(segment => segment.GetAttribute("style")));
		Assert.DoesNotContain("--_progress-color",
			ScopedCss.Declarations(Stylesheet, ".moka-progress-segment").Values.SelectMany(value => value.Split(' ')));
	}

	[Fact]
	public void TheBar_ReadsTheColor_WithPrimaryAsItsFallback() =>
		Assert.Equal(ReadsTheColor, ScopedCss.Declarations(Stylesheet, ".moka-progress-bar")["background"]);

	[Fact]
	public void TheRing_ReadsTheColor_WithPrimaryAsItsFallback() =>
		Assert.Equal(ReadsTheColor, ScopedCss.Declarations(Stylesheet, ".moka-progress-circle-bar")["stroke"]);

	// The new design restyles the track and the value, never the fill, so the color reaches both designs.
	[Fact]
	public void TheNewDesign_LeavesTheFillToTheColor()
	{
		Assert.DoesNotContain("background", ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-progress-bar").Keys);
		Assert.DoesNotContain("stroke", ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-progress-circle-bar").Keys);
	}

	[GeneratedRegex(@" b-\w+(="""")?")]
	private static partial Regex ScopeAttribute();

	// The line breaks between the two circles come from the .razor file, CRLF or LF depending on the checkout.
	[GeneratedRegex(@">\s+<")]
	private static partial Regex BetweenTags();
}
