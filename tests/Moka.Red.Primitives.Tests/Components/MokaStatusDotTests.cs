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

		IElement root = cut.Find(".moka-status-dot");
		Assert.Equal("status", root.GetAttribute("role"));
		Assert.NotNull(cut.Find(".moka-status-dot-dot"));
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
