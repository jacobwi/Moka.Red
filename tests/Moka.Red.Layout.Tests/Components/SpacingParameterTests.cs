using Bunit;
using Bunit.Rendering;
using Moka.Red.Layout.Accordion;
using Moka.Red.Layout.Container;
using Moka.Red.Layout.Flexbox;
using Moka.Red.Layout.Footer;
using Moka.Red.Layout.Grid;
using Moka.Red.Layout.Panel;
using Moka.Red.Layout.ScrollArea;
using Moka.Red.Layout.Sticky;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// Margin, Padding and Rounded are declared on every visual component, and these dropped some or
// all of them. Each case sets all three and checks where they land. Without them every style
// attribute has to read as it did before.
public class SpacingParameterTests : BunitContext
{
	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["Accordion"] = new(typeof(MokaAccordion), ""),
		["Container"] = new(typeof(MokaContainer), "max-width: 1200px; width: 100%; margin-left: auto; margin-right: auto; padding-left: var(--moka-spacing-lg); padding-right: var(--moka-spacing-lg)"),
		["Flexbox"] = new(typeof(MokaFlexbox), "display: flex; flex-direction: column; justify-content: flex-start; align-items: stretch"),
		["Footer"] = new(typeof(MokaFooter), "padding: var(--moka-spacing-sm) var(--moka-spacing-lg)"),
		["Grid"] = new(typeof(MokaGrid), "display: grid; grid-template-columns: repeat(1, 1fr)"),
		["Panel"] = new(typeof(MokaPanel), "", ("Title", "Panel")),
		["ScrollArea"] = new(typeof(MokaScrollArea), "overflow-x: hidden; overflow-y: auto"),
		["Sticky"] = new(typeof(MokaSticky), "position: sticky; top: 0; z-index: var(--moka-z-sticky)")
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
