using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.StatusDot;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaStatusDotTests : BunitContext
{
	[Fact]
	public void Renders_RootAndDot()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>();

		Assert.NotNull(cut.Find(".moka-status-dot"));
		Assert.Equal("true", cut.Find(".moka-status-dot-dot").GetAttribute("aria-hidden"));
	}

	// Every dot used to be a role="status" live region, so a page of them announced every change.
	[Fact]
	public void Default_IsNotALiveRegion()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Label, "ONLINE"));

		IElement root = cut.Find(".moka-status-dot");
		Assert.False(root.HasAttribute("role"));
		Assert.False(root.HasAttribute("aria-live"));
	}

	[Fact]
	public void Live_MakesItAStatusRegion()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Label, "saving...")
			.Add(x => x.Live, true));

		Assert.Equal("status", cut.Find(".moka-status-dot").GetAttribute("role"));
	}

	// A plain span with aria-label has no name screen readers use, so a named dot without a label
	// is exposed as an image.
	[Fact]
	public void DotOnly_WithAriaLabel_IsANamedImage()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.AddUnmatched("aria-label", "Online"));

		IElement root = cut.Find(".moka-status-dot");
		Assert.Equal("img", root.GetAttribute("role"));
		Assert.Equal("Online", root.GetAttribute("aria-label"));
	}

	[Fact]
	public void DotOnly_WithoutAName_HasNoRole()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>();

		Assert.False(cut.Find(".moka-status-dot").HasAttribute("role"));
	}

	[Fact]
	public void LabelledDot_KeepsItsTextInsteadOfBecomingAnImage()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Label, "ONLINE")
			.AddUnmatched("aria-label", "Server online"));

		Assert.False(cut.Find(".moka-status-dot").HasAttribute("role"));
	}

	[Fact]
	public void Default_IsSuccess_Md_Glow_Uppercase()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>();

		string cls = cut.Find(".moka-status-dot").ClassName!;
		Assert.Contains("moka-status-dot--success", cls, StringComparison.Ordinal);
		Assert.Contains("moka-status-dot--md", cls, StringComparison.Ordinal);
		Assert.Contains("moka-status-dot--glow", cls, StringComparison.Ordinal);
		Assert.Contains("moka-status-dot--uppercase", cls, StringComparison.Ordinal);
	}

	[Fact]
	public void Label_Renders()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Label, "ONLINE"));

		Assert.Equal("ONLINE", cut.Find(".moka-status-dot-label").TextContent);
	}

	[Fact]
	public void NoLabel_NoLabelSpan()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>();

		Assert.Empty(cut.FindAll(".moka-status-dot-label"));
	}

	[Fact]
	public void Pulse_AddsModifier()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Pulse, true));

		Assert.Contains("moka-status-dot--pulse", cut.Find(".moka-status-dot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Glow_False_RemovesModifier()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Glow, false));

		Assert.DoesNotContain("moka-status-dot--glow", cut.Find(".moka-status-dot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Uppercase_False_RemovesModifier()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Uppercase, false));

		Assert.DoesNotContain("moka-status-dot--uppercase", cut.Find(".moka-status-dot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Color_Override()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Color, MokaColor.Info));

		Assert.Contains("moka-status-dot--info", cut.Find(".moka-status-dot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Size_Override()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Size, MokaSize.Sm));

		Assert.Contains("moka-status-dot--sm", cut.Find(".moka-status-dot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void UserClass_Applied()
	{
		IRenderedComponent<MokaStatusDot> cut = Render<MokaStatusDot>(p => p
			.Add(x => x.Class, "my-dot"));

		Assert.Contains("my-dot", cut.Find(".moka-status-dot").ClassName, StringComparison.Ordinal);
	}
}
