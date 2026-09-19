using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.Button;
using Moka.Red.Primitives.Carousel;
using Moka.Red.Primitives.InfiniteCarousel;
using Moka.Red.Primitives.Media;
using Moka.Red.Primitives.Meter;
using Moka.Red.Primitives.Notice;
using Moka.Red.Primitives.SegmentedControl;
using Moka.Red.Primitives.Steps;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// These filled their container with width: 100%, so a margin made them wider than it. They take
// moka-fill-width now, which fills the container with the margin inside it, and their own rules set
// no width that would win over it. bUnit applies no CSS, so the rules are read as text.
public class FillWidthTests : BunitContext
{
	private static readonly Dictionary<string, (Type Component, (string, object?)[] Parameters, string Css, string Rule)>
		Cases = new()
		{
			["Notice"] = (typeof(MokaNotice), [], "MokaNotice.razor.css", ".moka-notice"),
			["Carousel"] = (typeof(MokaCarousel), [], "MokaCarousel.razor.css", ".moka-carousel"),
			["InfiniteCarousel"] = (typeof(MokaInfiniteCarousel), [("AutoPlay", false)],
				"MokaInfiniteCarousel.razor.css", ".moka-infinite-carousel"),
			["MediaGallery"] = (typeof(MokaMediaGallery), [], "MokaMediaGallery.razor.css", ".moka-media-gallery"),
			["VideoEmbed"] = (typeof(MokaVideoEmbed), [("Src", "clip.mp4")], "MokaVideoEmbed.razor.css",
				".moka-video-embed"),
			["Steps"] = (typeof(MokaSteps), [("Steps", new[] { "One", "Two" })], "MokaSteps.razor.css", ".moka-steps"),
			["SegmentedControlFullWidth"] = (typeof(MokaSegmentedControl), [("FullWidth", true)],
				"MokaSegmentedControl.razor.css", ".moka-segmented--full-width"),
			["ButtonFullWidth"] = (typeof(MokaButton), [("FullWidth", true)], "MokaButton.razor.css",
				".moka-btn--full-width"),
			["Meter"] = (typeof(MokaMeter), [("Value", 40d)], "MokaMeter.razor.css", ".moka-meter")
		};

	public FillWidthTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void TheRoot_FillsItsContainer_WithTheMarginInside(string name)
	{
		(Type component, (string, object?)[] parameters, string css, string rule) = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(RenderWithMargin(component, parameters));

		IElement root = cut.Nodes.OfType<IElement>().First();
		Assert.Contains("moka-fill-width", root.ClassList);
		Assert.Contains("margin: 7px", root.GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("width", ScopedCss.Declarations(css, rule).Keys);
	}

	// Only the full-width segmented control fills its container. The default one is as wide as its
	// segments.
	[Fact]
	public void AButtonWithoutFullWidth_KeepsItsOwnWidth()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>();

		Assert.DoesNotContain("moka-fill-width", cut.Find(".moka-btn").ClassList);
	}

	[Fact]
	public void TheDefaultSegmentedControl_KeepsItsOwnWidth()
	{
		IRenderedComponent<MokaSegmentedControl> cut = Render<MokaSegmentedControl>();

		Assert.DoesNotContain("moka-fill-width", cut.Find(".moka-segmented").ClassList);
	}

	private static RenderFragment RenderWithMargin(Type component, (string Name, object? Value)[] parameters) => builder =>
	{
		builder.OpenComponent(0, component);
		int sequence = 1;
		foreach ((string name, object? value) in parameters)
		{
			builder.AddAttribute(sequence++, name, value);
		}

		builder.AddAttribute(sequence, "MarginValue", "7px");
		builder.CloseComponent();
	};
}
