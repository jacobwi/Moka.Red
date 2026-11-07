using Bunit;
using Moka.Red.Core.Icons;
using Moka.Red.Primitives.Icon;

namespace Moka.Red.Primitives.Tests.Components;

// A size with no unit is invalid CSS. Chromium lets it through on an SVG, and a browser that doesn't drops the
// size, which drew MokaCommandBar's search icon 454 px square in the middle of the page.
public class MokaIconSizeTests : BunitContext
{
	private static readonly MokaIconDefinition Icon = new("test", "M0 0");

	[Theory]
	[InlineData("14", "width: 14px; height: 14px")]
	[InlineData("1.5", "width: 1.5px; height: 1.5px")]
	[InlineData("14px", "width: 14px; height: 14px")]
	[InlineData("1.5rem", "width: 1.5rem; height: 1.5rem")]
	[InlineData("calc(1rem + 2px)", "width: calc(1rem + 2px); height: calc(1rem + 2px)")]
	public void SizeValue_AlwaysReachesTheStyleWithAUnit(string sizeValue, string expected)
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p
			.Add(x => x.Icon, Icon)
			.Add(x => x.SizeValue, sizeValue));

		Assert.Contains(expected, cut.Find("svg").GetAttribute("style"), StringComparison.Ordinal);
	}
}
