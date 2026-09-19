using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.VirtualList;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// Margin, Padding and Rounded are declared on every visual component, and these dropped some or
// all of them. Each case sets all three and checks where they land. Without them every style
// attribute has to read as it did before.
public class SpacingParameterTests : BunitContext
{
	private static readonly RenderFragment<string> ItemText = item => builder => builder.AddContent(0, item);

	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["VirtualList"] = new(typeof(MokaVirtualList<string>),
			"height: 400px | height: 0px; flex-shrink: 0; | height: 20px | height: 0px; flex-shrink: 0; transform: translateY(999998000px);",
			("Items", new[] { "Ada" }), ("ItemTemplate", ItemText), ("ItemHeight", 20f))
	};

	public SpacingParameterTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void SpacingParameters_LandOnTheComponent(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));
		spacing.OpenIfNeeded(cut);

		spacing.AssertSpacing(cut);
	}

	[Theory]
	[MemberData(nameof(Names))]
	public void Styles_AreUnchanged_WithoutSpacingParameters(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: false));
		spacing.OpenIfNeeded(cut);

		Assert.Equal(spacing.DefaultStyles, SpacingCase.Styles(cut));
	}
}
