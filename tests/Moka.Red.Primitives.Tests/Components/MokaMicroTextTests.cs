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
		Assert.Equal("moka-label moka-label--micro moka-micro-label", label.ClassName);
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
		Assert.Equal("moka-caption moka-caption--micro moka-micro-label", caption.ClassName);
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

	[Fact]
	public void Micro_ReadsTheApprovedTokens()
	{
		IReadOnlyDictionary<string, string> css = ScopedCss.Declarations("moka.css", ".moka-micro-label");

		Assert.Equal("var(--moka-font-size-xs)", css["font-size"]);
		Assert.Equal("var(--moka-font-weight-bold)", css["font-weight"]);
		Assert.Equal("0.08em", css["letter-spacing"]);
		Assert.Equal("uppercase", css["text-transform"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", css["color"]);
	}

	// moka.css gives an h1 the title font on the new design, earlier in the same file and with the same specificity.
	[Fact]
	public void SharedMicroLabel_RestatesTheUiFont_AfterTheTitleFontRule()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "moka.css"));

		Assert.Equal("var(--moka-font-family)", ScopedCss.Declarations("moka.css", ".moka-micro-label")["font-family"]);
		Assert.True(css.IndexOf(".moka-micro-label {", StringComparison.Ordinal)
		            > css.IndexOf(".moka-gate :where(h1) {", StringComparison.Ordinal));
	}

	[Fact]
	public void Heading_ByDefault_IsNotMicro()
	{
		IRenderedComponent<MokaHeading> cut = Render<MokaHeading>(p => p.AddChildContent("Zones"));

		IElement heading = cut.Find("h2");
		Assert.Equal("moka-heading", heading.ClassName);
		Assert.Contains("font-size: var(--moka-font-size-xl)", heading.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(2, "h2")]
	[InlineData(3, "h3")]
	[InlineData(6, "h6")]
	public void Heading_Micro_KeepsTheElementItsLevelNames(int level, string element)
	{
		IRenderedComponent<MokaHeading> cut = Render<MokaHeading>(p => p
			.Add(x => x.Level, level)
			.Add(x => x.Micro, true)
			.AddChildContent("Zones"));

		IElement heading = cut.Find(element);
		Assert.Equal("moka-heading moka-heading--micro moka-micro-label", heading.ClassName);
		Assert.Equal("Zones", heading.TextContent);
	}

	[Fact]
	public void Heading_Micro_LeavesSizeWeightAndColorToTheSharedClass()
	{
		IRenderedComponent<MokaHeading> cut = Render<MokaHeading>(p => p
			.Add(x => x.Micro, true)
			.Add(x => x.SizeValue, "40px")
			.Add(x => x.Weight, MokaFontWeight.Normal)
			.Add(x => x.Color, MokaColor.Error)
			.Add(x => x.Align, MokaTextAlign.Center));

		Assert.Equal("line-height: var(--moka-line-height-tight); text-align: center; margin: 0",
			cut.Find("h2").GetAttribute("style"));
	}

	[Fact]
	public void Heading_Micro_KeepsTheConsumersClass()
	{
		IRenderedComponent<MokaHeading> cut = Render<MokaHeading>(p => p
			.Add(x => x.Micro, true)
			.Add(x => x.Class, "mine"));

		Assert.Equal("moka-heading moka-heading--micro moka-micro-label mine", cut.Find("h2").ClassName);
	}

	// Truncate added a class no stylesheet defined, so a long heading wrapped. It now takes moka.css's truncate utility.
	[Fact]
	public void Heading_Truncate_UsesTheSharedTruncateUtility()
	{
		IRenderedComponent<MokaHeading> cut = Render<MokaHeading>(p => p.Add(x => x.Truncate, true));

		Assert.Contains("moka-text--truncate", cut.Find("h2").ClassList);
		Assert.Equal("ellipsis", ScopedCss.Declarations("moka.css", ".moka-text--truncate")["text-overflow"]);
	}
}
