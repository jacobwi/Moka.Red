using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.Flexbox;

namespace Moka.Red.Layout.Tests.Components;

public class MokaFlexboxTests : BunitContext
{
	[Fact]
	public void Renders_DivElement()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		Assert.NotNull(div);
	}

	[Fact]
	public void Applies_RootClass()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find(".moka-flexbox");
		Assert.NotNull(div);
	}

	[Fact]
	public void Default_Direction_IsColumn()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		string? style = div.GetAttribute("style");
		Assert.Contains("flex-direction: column", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Row_Direction()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.Add(x => x.Direction, MokaDirection.Row)
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		string? style = div.GetAttribute("style");
		Assert.Contains("flex-direction: row", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Justify_Center()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.Add(x => x.Justify, MokaJustify.Center)
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		string? style = div.GetAttribute("style");
		Assert.Contains("justify-content: center", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Align_Center()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.Add(x => x.Align, MokaAlign.Center)
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		string? style = div.GetAttribute("style");
		Assert.Contains("align-items: center", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Wrap_Enabled()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.Add(x => x.Wrap, true)
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		string? style = div.GetAttribute("style");
		Assert.Contains("flex-wrap: wrap", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Gap_FromSpacingScale()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.Add(x => x.Gap, MokaSpacingScale.Md)
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		string? style = div.GetAttribute("style");
		Assert.Contains("gap:", style, StringComparison.Ordinal);
	}

	[Fact]
	public void GapValue_OverridesGapEnum()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.Add(x => x.Gap, MokaSpacingScale.Sm)
			.Add(x => x.GapValue, "20px")
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		string? style = div.GetAttribute("style");
		Assert.Contains("gap: 20px", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Inline_Flex()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.Add(x => x.Inline, true)
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		string? style = div.GetAttribute("style");
		Assert.Contains("display: inline-flex", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Appends_UserClass()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.Add(x => x.Class, "my-flex")
			.AddChildContent("<span>Child</span>"));

		IElement div = cut.Find("div");
		Assert.Contains("my-flex", div.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Renders_ChildContent()
	{
		IRenderedComponent<MokaFlexbox> cut = Render<MokaFlexbox>(p => p
			.AddChildContent("<span id=\"inner\">Hello</span>"));

		IElement inner = cut.Find("#inner");
		Assert.Equal("Hello", inner.TextContent);
	}
}
