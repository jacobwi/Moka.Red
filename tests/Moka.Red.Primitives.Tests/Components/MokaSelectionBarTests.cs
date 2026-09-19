using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.SelectionBar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaSelectionBarTests : BunitContext
{
	[Fact]
	public void Count_Zero_RendersNoToolbar()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 0));

		Assert.Empty(cut.FindAll(".moka-selection-bar"));
		Assert.Empty(cut.FindAll("[role=toolbar]"));
	}

	// Screen readers only announce changes to a live region that already exists. The bar was the
	// live region and was inserted already filled, so the first selection usually went unannounced.
	[Fact]
	public void LiveRegion_ExistsAndIsEmpty_BeforeAnythingIsSelected()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 0)
			.Add(x => x.Label, "files selected"));

		IElement status = cut.Find("[role=status]");
		Assert.Equal(string.Empty, status.TextContent);
		Assert.Contains("moka-visually-hidden", status.ClassList);
	}

	[Fact]
	public void LiveRegion_AnnouncesTheCountAndLabel_WhenASelectionStarts()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 0)
			.Add(x => x.Label, "files selected"));

		cut.Render(p => p.Add(x => x.Count, 3));

		Assert.Equal("3 files selected", cut.Find("[role=status]").TextContent);
	}

	[Fact]
	public void LiveRegion_Empties_WhenTheSelectionIsCleared()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 2));

		cut.Render(p => p.Add(x => x.Count, 0));

		Assert.Equal(string.Empty, cut.Find("[role=status]").TextContent);
	}

	// The toolbar holds buttons, so it must not be a live region itself.
	[Fact]
	public void Toolbar_IsNotALiveRegion()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 3));

		IElement toolbar = cut.Find("[role=toolbar]");
		Assert.False(toolbar.HasAttribute("aria-live"));
		Assert.Empty(toolbar.QuerySelectorAll("[role=status], [aria-live]"));
	}

	[Fact]
	public void Count_Positive_RendersToolbar()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 3));

		IElement bar = cut.Find(".moka-selection-bar");
		Assert.Equal("toolbar", bar.GetAttribute("role"));
	}

	[Fact]
	public void Count_ShownInCountSpan()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 7));

		Assert.Contains("7", cut.Find(".moka-selection-bar-count").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void Label_RendersAndAria()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 2)
			.Add(x => x.Label, "chosen"));

		Assert.Equal("chosen", cut.Find(".moka-selection-bar-label").TextContent);
		Assert.Equal("chosen", cut.Find(".moka-selection-bar").GetAttribute("aria-label"));
	}

	[Fact]
	public void ChildContent_RendersInActions()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 1)
			.AddChildContent("<button id=\"act\">Delete</button>"));

		IElement actions = cut.Find(".moka-selection-bar-actions");
		Assert.NotNull(actions.QuerySelector("#act"));
	}

	[Fact]
	public void ShowClear_Default_RendersButton()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 1));

		Assert.NotNull(cut.Find(".moka-selection-bar-clear"));
	}

	[Fact]
	public void ShowClear_False_NoButton()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 1)
			.Add(x => x.ShowClear, false));

		Assert.Empty(cut.FindAll(".moka-selection-bar-clear"));
	}

	[Fact]
	public void OnClear_InvokedOnClick()
	{
		var cleared = false;
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 1)
			.Add(x => x.OnClear, EventCallback.Factory.Create(this, () => cleared = true)));

		cut.Find(".moka-selection-bar-clear").Click();

		Assert.True(cleared);
	}

	[Fact]
	public void Fixed_AddsModifier()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 1)
			.Add(x => x.Fixed, true));

		Assert.Contains("moka-selection-bar--fixed", cut.Find(".moka-selection-bar").ClassName, StringComparison.Ordinal);
	}

	// A translateX(-50%) centered the fixed bar, so a margin pushed it off center. A dock along the
	// bottom of the viewport centers it with flexbox now, and the margin stays on the bar.
	[Fact]
	public void Fixed_SitsInACenteringDock_WithTheMarginOnTheBar()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 1)
			.Add(x => x.Fixed, true)
			.Add(x => x.MarginValue, "7px"));

		IElement bar = cut.Find(".moka-selection-bar-dock > .moka-selection-bar");
		Assert.Equal("margin: 7px", bar.GetAttribute("style"));
		Assert.Null(cut.Find(".moka-selection-bar-dock").GetAttribute("style"));

		IReadOnlyDictionary<string, string> dock =
			ScopedCss.Declarations("MokaSelectionBar.razor.css", ".moka-selection-bar-dock");
		Assert.Equal("fixed", dock["position"]);
		Assert.Equal("flex", dock["display"]);
		Assert.Equal("center", dock["justify-content"]);
		Assert.Equal("none", dock["pointer-events"]);

		IReadOnlyDictionary<string, string> fixedBar =
			ScopedCss.Declarations("MokaSelectionBar.razor.css", ".moka-selection-bar--fixed");
		Assert.DoesNotContain("transform", fixedBar.Keys);
		Assert.DoesNotContain("left", fixedBar.Keys);
		Assert.Equal("auto", fixedBar["pointer-events"]);
	}

	[Fact]
	public void NotFixed_HasNoDock()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 1));

		Assert.Empty(cut.FindAll(".moka-selection-bar-dock"));
		Assert.Single(cut.FindAll(".moka-selection-bar"));
	}
}
