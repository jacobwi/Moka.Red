using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.SelectionBar;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaSelectionBarTests : BunitContext
{
	[Fact]
	public void Count_Zero_RendersNothing()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 0));

		Assert.Empty(cut.FindAll(".moka-selection-bar"));
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
}
