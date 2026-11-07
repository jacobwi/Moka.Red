using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Typography;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// SpacingParameterTests pins the inline styles both have with Micro off.
public class MokaMicroTextTests : BunitContext
{
	[Fact]
	public void Label_ByDefault_IsNotMicro()
	{
		IRenderedComponent<MokaLabel> cut = Render<MokaLabel>();

		Assert.Equal("moka-label", cut.Find("label").ClassName);
	}

	[Fact]
	public void Label_Micro_LeavesTheLookToTheClass()
	{
		IRenderedComponent<MokaLabel> cut = Render<MokaLabel>(p => p
			.Add(x => x.Micro, true)
			.Add(x => x.Size, MokaSize.Lg)
			.Add(x => x.SizeValue, "40px")
			.Add(x => x.Weight, MokaFontWeight.Normal)
			.Add(x => x.Color, MokaColor.Error));

		IElement label = cut.Find("label");
		Assert.Equal("moka-label moka-label--micro", label.ClassName);
		Assert.Null(label.GetAttribute("style"));
	}

	[Fact]
	public void Label_Micro_KeepsAlignAndTheConsumersStyle()
	{
		IRenderedComponent<MokaLabel> cut = Render<MokaLabel>(p => p
			.Add(x => x.Micro, true)
			.Add(x => x.Align, MokaTextAlign.Right)
			.Add(x => x.Style, "max-width: 10rem"));

		Assert.Equal("text-align: right; max-width: 10rem", cut.Find("label").GetAttribute("style"));
	}

	[Fact]
	public void Label_WithoutMicro_StillAppliesColorAndWeight()
	{
		IRenderedComponent<MokaLabel> cut = Render<MokaLabel>(p => p
			.Add(x => x.Weight, MokaFontWeight.Bold)
			.Add(x => x.Color, MokaColor.Error));

		Assert.Equal(
			"font-size: var(--moka-font-size-base); font-weight: var(--moka-font-weight-bold); color: var(--moka-color-error)",
			cut.Find("label").GetAttribute("style"));
	}

	[Fact]
	public void Caption_ByDefault_IsNotMicro()
	{
		IRenderedComponent<MokaCaption> cut = Render<MokaCaption>();

		Assert.Equal("moka-caption", cut.Find("small").ClassName);
	}

	[Fact]
	public void Caption_Micro_DropsSizeColorAndOpacity()
	{
		IRenderedComponent<MokaCaption> cut = Render<MokaCaption>(p => p
			.Add(x => x.Micro, true)
			.Add(x => x.SizeValue, "40px"));

		IElement caption = cut.Find("small");
		Assert.Equal("moka-caption moka-caption--micro", caption.ClassName);
		Assert.Equal("line-height: var(--moka-line-height-base)", caption.GetAttribute("style"));
	}

	[Fact]
	public void Caption_Micro_IgnoresAColor()
	{
		IRenderedComponent<MokaCaption> cut = Render<MokaCaption>(p => p
			.Add(x => x.Micro, true)
			.Add(x => x.Color, MokaColor.Error));

		Assert.DoesNotContain("color", cut.Find("small").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void Caption_WithoutMicro_StillAppliesAColorWithNoOpacity()
	{
		IRenderedComponent<MokaCaption> cut = Render<MokaCaption>(p => p.Add(x => x.Color, MokaColor.Error));

		Assert.Equal(
			"font-size: var(--moka-font-size-xs); color: var(--moka-color-error); line-height: var(--moka-line-height-base)",
			cut.Find("small").GetAttribute("style"));
	}

	[Theory]
	[InlineData("MokaLabel.razor.css", ".moka-label--micro")]
	[InlineData("MokaCaption.razor.css", ".moka-caption--micro")]
	public void Micro_ReadsTheApprovedTokens(string file, string selector)
	{
		IReadOnlyDictionary<string, string> css = ScopedCss.Declarations(file, selector);

		Assert.Equal("var(--moka-font-size-xs)", css["font-size"]);
		Assert.Equal("var(--moka-font-weight-bold)", css["font-weight"]);
		Assert.Equal("0.08em", css["letter-spacing"]);
		Assert.Equal("uppercase", css["text-transform"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", css["color"]);
	}
}
