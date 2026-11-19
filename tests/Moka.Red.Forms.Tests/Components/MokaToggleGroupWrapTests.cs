using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.ToggleGroup;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// The group clipped items that didn't fit, so they could not be reached. It wraps now, and the separators sit in the gap
// between items so a group that fits on one row lays out as before. FullWidth and StackBelow work as on the segmented
// control.
public class MokaToggleGroupWrapTests : BunitContext
{
	private const string GroupCss = "MokaToggleGroup.razor.css";
	private const string ItemCss = "MokaToggleGroupItem.razor.css";

	private static RenderFragment Items => builder =>
	{
		foreach (string value in new[] { "a", "b", "c" })
		{
			builder.OpenComponent<MokaToggleGroupItem>(0);
			builder.AddComponentParameter(1, nameof(MokaToggleGroupItem.Value), value);
			builder.AddComponentParameter(2, nameof(MokaToggleGroupItem.Text), value.ToUpperInvariant());
			builder.CloseComponent();
		}
	};

	[Fact]
	public void ByDefault_TheMarkupIsAsBefore()
	{
		IRenderedComponent<MokaToggleGroup> cut = Render<MokaToggleGroup>(p => p.Add(x => x.ChildContent, Items));

		IElement group = cut.Find(".moka-toggle-group");
		Assert.Equal("moka-toggle-group moka-toggle-group--md moka-toggle-group--primary", group.ClassName);
		Assert.Null(group.GetAttribute("style"));
		Assert.Equal("group", group.GetAttribute("role"));
	}

	[Fact]
	public void TheGroup_WrapsRatherThanClipping_WithASeparatorWideGap()
	{
		IReadOnlyDictionary<string, string> group = ScopedCss.Declarations(GroupCss, ".moka-toggle-group");

		Assert.Equal("inline-flex", group["display"]);
		Assert.Equal("wrap", group["flex-wrap"]);
		Assert.Equal("var(--moka-border-width)", group["gap"]);
		Assert.Equal("hidden", group["overflow"]);
	}

	// Each separator lies in the gap, given back by the margin, so a row that fits is as wide as before; on a row that
	// wraps, the last item's separator is pushed past the group's edge and clipped.
	[Fact]
	public void AnItemsSeparator_LiesInTheGap_AndItsRowLineUnderIt()
	{
		IReadOnlyDictionary<string, string> item = ScopedCss.Declarations(ItemCss, ".moka-toggle-group-item");

		Assert.Equal("var(--moka-border-width) solid var(--_toggle-rule)", item["border-right"]);
		Assert.Equal("calc(-1 * var(--moka-border-width))", item["margin-right"]);
		Assert.Equal("0 var(--moka-border-width) 0 0 var(--_toggle-rule)", item["box-shadow"]);
		Assert.Equal("max-content", item["min-width"]);
		Assert.Equal("var(--moka-color-outline-variant)", item["--_toggle-rule"]);

		IReadOnlyDictionary<string, string> last = ScopedCss.Declarations(ItemCss, ".moka-toggle-group-item:last-child");
		Assert.Equal("none", last["border-right"]);
		Assert.Equal("0", last["margin-right"]);
	}

	[Fact]
	public void OnTheNewDesign_TheSeparatorsKeepTheirColor() =>
		Assert.Equal("var(--moka-color-outline)",
			ScopedCss.Declarations(ItemCss, ".moka-gate .moka-toggle-group-item")["--_toggle-rule"]);

	[Fact]
	public void FullWidth_FillsTheContainer_AndTheItemsShareIt()
	{
		IRenderedComponent<MokaToggleGroup> cut = Render<MokaToggleGroup>(p => p
			.Add(x => x.FullWidth, true)
			.Add(x => x.ChildContent, Items));

		IElement group = cut.Find(".moka-toggle-group");
		Assert.Contains("moka-toggle-group--full-width", group.ClassList);
		Assert.Contains("moka-fill-width", group.ClassList);
		Assert.Equal("flex", ScopedCss.Declarations(GroupCss, ".moka-toggle-group--full-width")["display"]);
		Assert.Equal("1 1 0%", ScopedCss.Declarations(GroupCss, ".moka-toggle-group--full-width ::deep .moka-toggle-group-item")["flex"]);
	}

	[Theory]
	[InlineData("30rem")]
	[InlineData("calc(20rem + 4vw)")]
	[InlineData("var(--stack-at)")]
	public void StackBelow_WithFullWidth_WritesTheWidth(string width)
	{
		IRenderedComponent<MokaToggleGroup> cut = Render<MokaToggleGroup>(p => p
			.Add(x => x.FullWidth, true)
			.Add(x => x.StackBelow, width)
			.Add(x => x.ChildContent, Items));

		IElement group = cut.Find(".moka-toggle-group");
		Assert.Contains("moka-toggle-group--stack", group.ClassList);
		Assert.Equal($"--_stack-below: {width}", group.GetAttribute("style"));
	}

	[Fact]
	public void StackBelow_WithoutFullWidth_DoesNothing()
	{
		IRenderedComponent<MokaToggleGroup> cut = Render<MokaToggleGroup>(p => p
			.Add(x => x.StackBelow, "30rem")
			.Add(x => x.ChildContent, Items));

		IElement group = cut.Find(".moka-toggle-group");
		Assert.DoesNotContain("moka-toggle-group--stack", group.ClassList);
		Assert.Null(group.GetAttribute("style"));
	}

	[Theory]
	[InlineData("30rem; color: red")]
	[InlineData("url(x)")]
	[InlineData("wide")]
	public void StackBelow_ThatIsntALength_IsIgnored(string width)
	{
		IRenderedComponent<MokaToggleGroup> cut = Render<MokaToggleGroup>(p => p
			.Add(x => x.FullWidth, true)
			.Add(x => x.StackBelow, width)
			.Add(x => x.ChildContent, Items));

		IElement group = cut.Find(".moka-toggle-group");
		Assert.DoesNotContain("moka-toggle-group--stack", group.ClassList);
		Assert.Null(group.GetAttribute("style"));
	}

	// Under the width a basis of (width - 100%) * 999 is a whole row; over it it clamps to zero and the items share it.
	[Fact]
	public void StackBelow_SwitchesTheItemsBasisAtTheWidth() =>
		Assert.Equal("calc((var(--_stack-below) - 100%) * 999)",
			ScopedCss.Declarations(GroupCss, ".moka-toggle-group--stack ::deep .moka-toggle-group-item")["flex-basis"]);
}
