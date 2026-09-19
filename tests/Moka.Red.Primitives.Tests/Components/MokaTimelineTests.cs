using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.Timeline;

namespace Moka.Red.Primitives.Tests.Components;

// The alternate layout is CSS only (MokaTimeline.razor.css), which bUnit does not apply, so it is
// checked in a browser. These tests cover the dot colours.
public class MokaTimelineTests : BunitContext
{
	[Fact]
	public void DefaultDot_HasNoInlineColours()
	{
		IRenderedComponent<MokaTimelineItem> cut = Render<MokaTimelineItem>(p => p
			.Add(x => x.Title, "Order placed")
			.Add(x => x.Icon, MokaIcons.Status.CheckCircle));

		Assert.Null(cut.Find(".moka-timeline-item__dot").GetAttribute("style"));
	}

	// The icon used to keep the on-primary colour whatever DotColor was.
	[Theory]
	[InlineData(MokaColor.Success, "success")]
	[InlineData(MokaColor.Warning, "warning")]
	[InlineData(MokaColor.Info, "info")]
	[InlineData(MokaColor.Secondary, "secondary")]
	public void DotColor_GivesTheIconTheMatchingOnColour(MokaColor color, string name)
	{
		IRenderedComponent<MokaTimelineItem> cut = Render<MokaTimelineItem>(p => p
			.Add(x => x.Icon, MokaIcons.Status.CheckCircle)
			.Add(x => x.DotColor, color));

		string style = DotStyle(cut);
		Assert.Contains($"background-color: var(--moka-color-{name})", style, StringComparison.Ordinal);
		Assert.Contains($"border-color: var(--moka-color-{name})", style, StringComparison.Ordinal);
		Assert.Contains($"color: var(--moka-color-on-{name})", style, StringComparison.Ordinal);
	}

	// A surface dot used to take the surface colour for its border too, so it vanished into the page.
	[Fact]
	public void SurfaceDot_GetsAnOutline_AndTheOnSurfaceIcon()
	{
		IRenderedComponent<MokaTimelineItem> cut = Render<MokaTimelineItem>(p => p
			.Add(x => x.Icon, MokaIcons.Status.Info)
			.Add(x => x.DotColor, MokaColor.Surface));

		string style = DotStyle(cut);
		Assert.Contains("background-color: var(--moka-color-surface)", style, StringComparison.Ordinal);
		Assert.Contains("border-color: var(--moka-color-outline)", style, StringComparison.Ordinal);
		Assert.Contains("color: var(--moka-color-on-surface)", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Alternate_AddsTheModifier()
	{
		IRenderedComponent<MokaTimeline> cut = Render<MokaTimeline>(p => p
			.Add(x => x.Alternate, true)
			.AddChildContent<MokaTimelineItem>(item => item.Add(x => x.Title, "One"))
			.AddChildContent<MokaTimelineItem>(item => item.Add(x => x.Title, "Two")));

		IElement timeline = cut.Find(".moka-timeline");
		Assert.Contains("moka-timeline--alternate", timeline.ClassList);
		Assert.Equal(2, timeline.QuerySelectorAll(":scope > .moka-timeline-item").Length);
	}

	private static string DotStyle(IRenderedComponent<MokaTimelineItem> cut) =>
		cut.Find(".moka-timeline-item__dot").GetAttribute("style") ?? string.Empty;
}
