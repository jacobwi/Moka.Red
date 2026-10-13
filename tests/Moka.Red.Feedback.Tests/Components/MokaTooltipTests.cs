using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaTooltipTests : BunitContext
{
	[Fact]
	public void Renders_TriggerWrapper()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Tooltip text")
			.AddChildContent("<button>Hover me</button>"));

		IElement trigger = cut.Find(".moka-tooltip-trigger");
		Assert.NotNull(trigger);
	}

	[Fact]
	public void Text_RendersInPopup()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Help text")
			.AddChildContent("<span>Info</span>"));

		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.Equal("Help text", popup.TextContent);
	}

	[Fact]
	public void HasRoleTooltip()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Accessible")
			.AddChildContent("<span>Trigger</span>"));

		IElement popup = cut.Find("[role='tooltip']");
		Assert.NotNull(popup);
	}

	[Fact]
	public void NoTextOrContent_NoPopup()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.AddChildContent("<span>No tooltip</span>"));

		Assert.Empty(cut.FindAll(".moka-tooltip-popup"));
	}

	[Fact]
	public void Position_Top_ByDefault()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Top")
			.AddChildContent("<span>Trigger</span>"));

		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.Contains("moka-tooltip-popup--top", popup.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Position_Bottom()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Bottom")
			.Add(x => x.Position, MokaTooltipPosition.Bottom)
			.AddChildContent("<span>Trigger</span>"));

		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.Contains("moka-tooltip-popup--bottom", popup.ClassName, StringComparison.Ordinal);
	}

	// A variable rather than transition-delay itself: the shown state reads it, so showing waits and hiding does not.
	[Fact]
	public void Delay_AppliedAsStyle()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Delayed")
			.Add(x => x.Delay, 500)
			.AddChildContent("<span>Trigger</span>"));

		IElement popup = cut.Find(".moka-tooltip-popup");
		string? style = popup.GetAttribute("style");
		Assert.Contains("--moka-tooltip-delay: 500ms", style, StringComparison.Ordinal);
		Assert.DoesNotContain("transition-delay", style, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePopupIsPlacedAgainstItsOwnTrigger()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Anchored")
			.AddChildContent("<span>Trigger</span>"));

		string anchor = StyleValue(cut.Find(".moka-tooltip-trigger"), "anchor-name");
		Assert.StartsWith("--moka-tooltip-", anchor, StringComparison.Ordinal);
		Assert.Equal(anchor, StyleValue(cut.Find(".moka-tooltip-popup"), "position-anchor"));
	}

	// One shared name would place every popup against whichever trigger carried it last.
	[Fact]
	public void EveryTooltipNamesItsOwnAnchor()
	{
		IRenderedComponent<MokaTooltip> first = Render<MokaTooltip>(p => p.Add(x => x.Text, "One"));
		IRenderedComponent<MokaTooltip> second = Render<MokaTooltip>(p => p.Add(x => x.Text, "Two"));

		Assert.NotEqual(
			StyleValue(first.Find(".moka-tooltip-trigger"), "anchor-name"),
			StyleValue(second.Find(".moka-tooltip-trigger"), "anchor-name"));
	}

	[Fact]
	public void AStyleOfTheCallersKeepsTheAnchor()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Styled")
			.Add(x => x.Style, "margin-left: 8px"));

		string? style = cut.Find(".moka-tooltip-trigger").GetAttribute("style");
		Assert.Contains("anchor-name: --moka-tooltip-", style, StringComparison.Ordinal);
		Assert.Contains("margin-left: 8px", style, StringComparison.Ordinal);
	}

	private static string StyleValue(IElement element, string property)
	{
		foreach (string declaration in (element.GetAttribute("style") ?? "").Split(';'))
		{
			int colon = declaration.IndexOf(':', StringComparison.Ordinal);
			if (colon > 0 && declaration[..colon].Trim() == property)
			{
				return declaration[(colon + 1)..].Trim();
			}
		}

		return "";
	}

	[Fact]
	public void ChildContent_Renders()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Tip")
			.AddChildContent("<button id=\"btn\">Click</button>"));

		IElement btn = cut.Find("#btn");
		Assert.Equal("Click", btn.TextContent);
	}
}
