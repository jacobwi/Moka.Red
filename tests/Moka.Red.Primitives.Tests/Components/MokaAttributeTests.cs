using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Primitives.Attribute;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public partial class MokaAttributeTests : BunitContext
{
	// With nothing to do, a button would be a tab stop that does nothing.
	[Fact]
	public void AnAttributeWithNothingToDo_IsASpan_OutOfTheTabOrder()
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Label, "Status")
			.AddChildContent("Active"));

		IElement root = cut.Find(".moka-attr");
		Assert.Equal("SPAN", root.TagName);
		Assert.False(root.HasAttribute("tabindex"));
		Assert.False(root.HasAttribute("aria-pressed"));
		Assert.DoesNotContain("moka-attr--clickable", root.ClassList);
		Assert.Empty(cut.FindAll("button"));
	}

	[Fact]
	public async Task AClickableAttribute_IsAButton_ThatIsNoToggle()
	{
		int clicks = 0;
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, _ => clicks++)
			.AddChildContent("Filter"));

		IElement root = cut.Find(".moka-attr");
		Assert.Equal("BUTTON", root.TagName);
		Assert.Equal("button", root.GetAttribute("type"));
		Assert.False(root.HasAttribute("aria-pressed"));

		await root.ClickAsync(new MouseEventArgs());

		Assert.Equal(1, clicks);
	}

	[Fact]
	public async Task ASelectableAttribute_IsAToggleButton_WithAriaPressedAsText()
	{
		bool? reported = null;
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Selectable, true)
			.Add(x => x.Selected, false)
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, v => reported = v))
			.AddChildContent("Active"));

		Assert.Equal("false", cut.Find("button.moka-attr").GetAttribute("aria-pressed"));
		Assert.Empty(cut.FindAll(".moka-attr-check"));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.True(reported);
		Assert.Equal("true", cut.Find("button.moka-attr").GetAttribute("aria-pressed"));
		Assert.NotNull(cut.Find("button.moka-attr .moka-attr-check"));
	}

	// A dismissible attribute was a button holding its remove button, which isn't valid HTML: the parser closes the
	// outer button at the start of the inner one, so a page rendered on the server put the remove button outside the
	// attribute. bUnit's parser does the same, and the markup it is given is read here before any parsing.
	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public void ADismissibleAttribute_KeepsItsRemoveButtonOutOfAnyOtherButton(bool clickable, bool selectable)
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Dismissible, true)
			.Add(x => x.Clickable, clickable)
			.Add(x => x.Selectable, selectable)
			.Add(x => x.Selected, true)
			.AddChildContent("React"));

		IElement root = cut.Find(".moka-attr");
		IElement remove = cut.Find(".moka-attr-dismiss");
		Assert.Equal("SPAN", root.TagName);
		Assert.Contains("moka-attr--dismissible", root.ClassList);
		Assert.NotNull(remove.Closest(".moka-attr"));
		Assert.All(cut.FindAll("button"), button => Assert.Null(button.ParentElement!.Closest("button")));
		Assert.Equal(clickable || selectable ? 2 : 1, cut.FindAll("button").Count);
		Assert.Equal(1, DeepestButton(cut.Markup));
	}

	[Fact]
	public void ADismissibleOnlyAttribute_HasNoButtonOfItsOwn()
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Dismissible, true)
			.AddChildContent("React"));

		Assert.Empty(cut.FindAll(".moka-attr-action"));
		Assert.DoesNotContain("moka-attr--clickable", cut.Find(".moka-attr").ClassList);
		Assert.Equal("React", cut.Find(".moka-attr > .moka-attr-content").TextContent);
	}

	[Fact]
	public void TheRemoveButton_IsATabStop_NamedForTheLabel()
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Label, "Status")
			.Add(x => x.Dismissible, true)
			.AddChildContent("Active"));

		IElement remove = cut.Find(".moka-attr-dismiss");
		Assert.False(remove.HasAttribute("tabindex"));
		Assert.False(remove.HasAttribute("disabled"));
		Assert.Equal("button", remove.GetAttribute("type"));
		IconTooltip.AssertNamed(remove, "Remove Status", MokaTooltipPosition.Bottom);
	}

	// The content is markup the attribute can't read, so without a label there is nothing to add to the name.
	[Fact]
	public void TheRemoveButton_OfAnAttributeWithNoLabel_IsNamedRemove()
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Dismissible, true)
			.AddChildContent("React"));

		IconTooltip.AssertNamed(cut.Find(".moka-attr-dismiss"), "Remove", MokaTooltipPosition.Bottom);
	}

	[Theory]
	[InlineData("Remove React", "Remove React")]
	[InlineData("Supprimer React", "Supprimer React")]
	[InlineData(" ", "Remove Framework")]
	[InlineData(null, "Remove Framework")]
	public void DismissLabel_NamesTheRemoveButton(string? dismissLabel, string expected)
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Label, "Framework")
			.Add(x => x.Dismissible, true)
			.Add(x => x.DismissLabel, dismissLabel)
			.AddChildContent("React"));

		IconTooltip.AssertNamed(cut.Find(".moka-attr-dismiss"), expected, MokaTooltipPosition.Bottom);
	}

	[Fact]
	public async Task ADisabledDismissibleAttribute_DisablesBothButtons_AndRemovesNothing()
	{
		bool removed = false;
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.Dismissible, true)
			.Add(x => x.Disabled, true)
			.Add(x => x.OnDismiss, EventCallback.Factory.Create(this, () => removed = true))
			.AddChildContent("React"));

		Assert.Equal(2, cut.FindAll("button").Count);
		Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));

		await cut.Find(".moka-attr-dismiss").ClickAsync(new MouseEventArgs());

		Assert.False(removed);
	}

	[Fact]
	public async Task ADismissibleAttributeWithAnAction_ClicksAndRemovesApart()
	{
		int clicks = 0;
		int removes = 0;
		bool? reported = null;
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Label, "Status")
			.Add(x => x.Clickable, true)
			.Add(x => x.Selectable, true)
			.Add(x => x.Dismissible, true)
			.Add(x => x.Selected, false)
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, v => reported = v))
			.Add(x => x.OnClick, _ => clicks++)
			.Add(x => x.OnDismiss, EventCallback.Factory.Create(this, () => removes++))
			.AddChildContent("Active"));

		IElement action = cut.Find(".moka-attr > .moka-attr-action");
		Assert.Equal("button", action.GetAttribute("type"));
		Assert.Equal("false", action.GetAttribute("aria-pressed"));
		Assert.NotNull(action.QuerySelector(".moka-attr-label"));
		Assert.NotNull(action.QuerySelector(".moka-attr-content"));
		Assert.Empty(cut.FindAll(".moka-attr-check"));

		await action.ClickAsync(new MouseEventArgs());

		Assert.Equal(1, clicks);
		Assert.True(reported);
		Assert.Equal("true", cut.Find(".moka-attr-action").GetAttribute("aria-pressed"));
		Assert.Contains("moka-attr--selected", cut.Find(".moka-attr").ClassList);
		Assert.NotNull(cut.Find(".moka-attr > .moka-attr-check"));

		await cut.Find(".moka-attr-dismiss").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, removes);
		Assert.Equal(1, clicks);
		Assert.Equal("true", cut.Find(".moka-attr-action").GetAttribute("aria-pressed"));
	}

	// The remove button comes after the content and before the check, as it did inside the old button, so the look
	// doesn't move.
	[Fact]
	public void ADismissibleSelectedAttribute_KeepsItsPartsInTheOldOrder()
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Selectable, true)
			.Add(x => x.Selected, true)
			.Add(x => x.Dismissible, true)
			.AddChildContent("Pinned"));

		string[] order = cut.Find(".moka-attr").Children
			.Select(child => child.ClassList.Contains("moka-attr-action") ? "action"
				: child.QuerySelector(".moka-attr-dismiss") is not null ? "remove"
				: child.ClassList.Contains("moka-attr-check") ? "check"
				: child.ClassName ?? "")
			.ToArray();
		Assert.Equal(["action", "remove", "check"], order);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void IdClassStyleAndAttributes_GoOnTheOutermostElement(bool dismissible)
	{
		IRenderedComponent<MokaAttribute> cut = Render<MokaAttribute>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.Dismissible, dismissible)
			.Add(x => x.Id, "attr-1")
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "order: 2")
			.AddUnmatched("data-test", "attr")
			.AddChildContent("React"));

		IElement root = cut.Find(".moka-attr");
		Assert.Null(root.ParentElement!.Closest(".moka-attr"));
		Assert.Equal("attr-1", root.Id);
		Assert.EndsWith("mine", root.ClassName, StringComparison.Ordinal);
		Assert.Contains("order: 2", root.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("attr", root.GetAttribute("data-test"));
		Assert.Single(cut.FindAll("#attr-1"));
		Assert.Single(cut.FindAll("[data-test]"));
	}

	[Fact]
	public void TheStylesheet_CoversTheAttributeWithItsButton_AndRingsBothButtons()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaAttribute.razor.css")).ReplaceLineEndings("\n");

		Assert.Contains(".moka-attr:focus-visible,\n.moka-attr:has(> .moka-attr-action:focus-visible) {", css, StringComparison.Ordinal);
		Assert.Equal("relative", ScopedCss.Declarations("MokaAttribute.razor.css", ".moka-attr--dismissible")["position"]);
		IReadOnlyDictionary<string, string> cover = ScopedCss.Declarations("MokaAttribute.razor.css", ".moka-attr-action::after");
		Assert.Equal("absolute", cover["position"]);
		Assert.Equal("0", cover["inset"]);
		IReadOnlyDictionary<string, string> action = ScopedCss.Declarations("MokaAttribute.razor.css", ".moka-attr-action");
		Assert.Equal("0", action["padding"]);
		Assert.Equal("inherit", action["gap"]);
		Assert.Equal("inherit", action["font"]);
		Assert.Equal("var(--moka-focus-ring)", ScopedCss.Declarations("MokaAttribute.razor.css", ".moka-attr-dismiss:focus-visible")["box-shadow"]);
	}

	// A filter or transform on the attribute becomes the containing block of the remove button's fixed tooltip, which
	// then landed over the button and to its left (gotcha 30). Every hover or press rule that sets one has to stand
	// down while the remove button is pointed at or focused.
	[Fact]
	public void HoverAndPressFilters_StandDownForTheRemoveButton()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaAttribute.razor.css")).ReplaceLineEndings("\n");
		string[] rules =
		[
			.. CssRule().Matches(css)
				.Where(rule => rule.Groups["selector"].Value.Contains(":hover", StringComparison.Ordinal) ||
				               rule.Groups["selector"].Value.Contains(":active", StringComparison.Ordinal))
				.Where(rule => FilterOrTransform().IsMatch(rule.Groups["body"].Value))
				.Select(rule => rule.Groups["selector"].Value.Trim())
		];

		Assert.Equal(2, rules.Length);
		Assert.All(rules, selector =>
			Assert.EndsWith(":not(:has(.moka-attr-dismiss:is(:hover, :focus-visible)))", selector, StringComparison.Ordinal));
	}

	[GeneratedRegex(@"(?<selector>[^{}]+)\{(?<body>[^{}]*)\}")]
	private static partial Regex CssRule();

	[GeneratedRegex(@"(^|;|\s)(filter|transform)\s*:")]
	private static partial Regex FilterOrTransform();

	private static int DeepestButton(string markup)
	{
		int depth = 0;
		int deepest = 0;
		foreach (Match tag in ButtonTag().Matches(markup))
		{
			depth += tag.Value.StartsWith("</", StringComparison.Ordinal) ? -1 : 1;
			deepest = Math.Max(deepest, depth);
		}

		return deepest;
	}

	[GeneratedRegex(@"</?button\b")]
	private static partial Regex ButtonTag();
}
