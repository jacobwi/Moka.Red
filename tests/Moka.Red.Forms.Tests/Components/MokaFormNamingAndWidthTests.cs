using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.Common;
using Moka.Red.Forms.RadioGroup;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// A radio group took its accessible name from Label, which also draws a visible heading, so a group
// under someone else's heading could not be named without drawing a second one.
public class MokaRadioGroupNamingTests : BunitContext
{
	[Fact]
	public void WithoutAnAriaLabel_TheVisibleLabelNamesTheGroup()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Label, "Theme"));

		Assert.Equal("Theme", cut.Find("[role=radiogroup]").GetAttribute("aria-label"));
	}

	[Fact]
	public void AnAriaLabelNamesTheGroupInstead()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Label, "Theme")
			.Add(x => x.AriaLabel, "Colour theme"));

		Assert.Equal("Colour theme", cut.Find("[role=radiogroup]").GetAttribute("aria-label"));
	}

	// The point of it: a group with no heading of its own still has a name.
	[Fact]
	public void AGroupWithNoVisibleLabel_CanStillBeNamed()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.AriaLabel, "Colour theme"));

		Assert.Equal("Colour theme", cut.Find("[role=radiogroup]").GetAttribute("aria-label"));
		Assert.Empty(cut.FindAll(".moka-field-label"));
	}
}

// A control that <label for> cannot name points aria-labelledby at the field wrapper's label, and
// needed a supported way to work out that id.
public class MokaFieldWrapperLabelIdTests : BunitContext
{
	[Fact]
	public void TheLabelIdIsThePublishedOne()
	{
		IRenderedComponent<MokaFieldWrapper> cut = Render<MokaFieldWrapper>(p => p
			.Add(x => x.Label, "Mode")
			.Add(x => x.InputId, "mode")
			.Add(x => x.CustomControl, true));

		IElement label = cut.Find(".moka-field-label");
		Assert.Equal(MokaFieldWrapper.LabelIdFor("mode"), label.Id);
		// A custom control is not a native one, so the label carries no for.
		Assert.False(label.HasAttribute("for"));
	}
}

// Inside a flex row the box asked for the search input's intrinsic width and would not shrink below it.
public class MokaSearchInputWidthTests
{
	[Fact]
	public void TheBoxFollowsItsContainer()
	{
		IReadOnlyDictionary<string, string> root =
			ScopedCss.Declarations("MokaSearchInput.razor.css", ".moka-search");

		Assert.Equal("100%", root["width"]);
		Assert.Equal("0", root["min-width"]);
	}
}
