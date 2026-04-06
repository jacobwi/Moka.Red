using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.Icon;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaIconTests : BunitContext
{
	[Fact]
	public void Renders_SvgElement()
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Save));

		IElement svg = cut.Find("svg");
		Assert.NotNull(svg);
		Assert.Equal("true", svg.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void Renders_PathWithSvgData()
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Save));

		IElement path = cut.Find("path");
		string? d = path.GetAttribute("d");
		Assert.NotNull(d);
		Assert.NotEmpty(d);
	}

	[Fact]
	public void Applies_RootClass()
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Save));

		IElement svg = cut.Find("svg");
		Assert.Contains("moka-icon", svg.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Applies_SizeClass()
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Save)
			.Add(x => x.Size, MokaSize.Lg));

		IElement svg = cut.Find("svg");
		Assert.Contains("moka-icon--lg", svg.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Applies_CustomSizeValue()
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Save)
			.Add(x => x.SizeValue, "48px"));

		IElement svg = cut.Find("svg");
		string? style = svg.GetAttribute("style");
		Assert.Contains("width: 48px", style, StringComparison.Ordinal);
		Assert.Contains("height: 48px", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Applies_ColorClass()
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Save)
			.Add(x => x.Color, MokaColor.Error));

		IElement svg = cut.Find("svg");
		Assert.Contains("moka-icon--error", svg.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Appends_UserClass()
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Save)
			.Add(x => x.Class, "extra-class"));

		IElement svg = cut.Find("svg");
		Assert.Contains("extra-class", svg.ClassName, StringComparison.Ordinal);
	}
}
