using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Icons;
using Moka.Red.Feedback.EmptyState;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public partial class MokaEmptyStateLookTests : BunitContext
{
	private const string Stylesheet = "MokaEmptyState.razor.css";

	private static readonly MokaIconDefinition SearchIcon = MokaIcons.Action.Search;

	[Fact]
	public void Default_KeepsItsMarkup()
	{
		IRenderedComponent<MokaEmptyState> cut = Render<MokaEmptyState>(p => p
			.Add(x => x.Title, "Nothing here")
			.Add(x => x.Description, "Try again")
			.AddChildContent("<button>Clear</button>"));

		Assert.Equal(
			"""<div class="moka-empty-state"><div class="moka-empty-state__title">Nothing here</div>""" +
			"""<div class="moka-empty-state__description">Try again</div>""" +
			"""<div class="moka-empty-state__actions"><button>Clear</button></div></div>""",
			ScopeAttribute().Replace(cut.Markup, ""));
	}

	[Fact]
	public void Icon_FollowsTheCustomProperty_AndFallsBackTo48()
	{
		IRenderedComponent<MokaEmptyState> cut = Render<MokaEmptyState>(p => p.Add(x => x.Icon, SearchIcon));

		string? style = cut.Find(".moka-empty-state__icon svg").GetAttribute("style");

		Assert.Contains("width: var(--_empty-icon, 48px); height: var(--_empty-icon, 48px)", style, StringComparison.Ordinal);
	}

	// The Matrix look keeps its 48px icon only while nothing outside a compact row or a Gate rule sets the property.
	[Fact]
	public void IconSize_IsNotSetOnTheMatrixLook()
	{
		Assert.DoesNotContain("--_empty-icon", ScopedCss.Declarations(Stylesheet, ".moka-empty-state__icon").Keys);
	}

	[Fact]
	public void Live_IsAPoliteStatus()
	{
		IElement root = Render<MokaEmptyState>(p => p.Add(x => x.Live, true)).Find(".moka-empty-state");

		Assert.Equal("status", root.GetAttribute("role"));
		Assert.Equal("polite", root.GetAttribute("aria-live"));
	}

	[Fact]
	public void Live_IsOffByDefault()
	{
		IElement root = Render<MokaEmptyState>().Find(".moka-empty-state");

		Assert.Null(root.GetAttribute("role"));
		Assert.Null(root.GetAttribute("aria-live"));
	}

	[Fact]
	public void Framed_AddsItsClass()
	{
		IElement root = Render<MokaEmptyState>(p => p.Add(x => x.Framed, true)).Find(".moka-empty-state");

		Assert.Equal("moka-empty-state moka-empty-state--framed", root.ClassName);
	}

	[Fact]
	public void FramedRule_IsADashedOutline()
	{
		IReadOnlyDictionary<string, string> framed = ScopedCss.Declarations(Stylesheet, ".moka-empty-state--framed");

		Assert.Equal("1px dashed var(--moka-color-outline)", framed["border"]);
		Assert.Equal("var(--moka-radius-lg)", framed["border-radius"]);
	}

	[Fact]
	public void Compact_HoldsTheTitleAndDescriptionTogether_WithTheActionsAfter()
	{
		IRenderedComponent<MokaEmptyState> cut = Render<MokaEmptyState>(p => p
			.Add(x => x.Compact, true)
			.Add(x => x.Icon, SearchIcon)
			.Add(x => x.Title, "No rules yet")
			.Add(x => x.Description, "The defaults apply.")
			.AddChildContent("<button>Add one</button>"));

		IElement root = cut.Find(".moka-empty-state");

		Assert.Contains("moka-empty-state--compact", root.ClassList);
		Assert.Equal(
			["moka-empty-state__icon", "moka-empty-state__text", "moka-empty-state__actions"],
			root.Children.Select(child => child.ClassName));
		Assert.Equal(
			["moka-empty-state__title", "moka-empty-state__description"],
			root.Children[1].Children.Select(child => child.ClassName));
	}

	[Fact]
	public void NotCompact_HasNoTextWrapper()
	{
		IRenderedComponent<MokaEmptyState> cut = Render<MokaEmptyState>(p => p.Add(x => x.Title, "Nothing here"));

		Assert.Empty(cut.FindAll(".moka-empty-state__text"));
	}

	[Fact]
	public void CompactRule_IsARowLeftAligned()
	{
		IReadOnlyDictionary<string, string> compact = ScopedCss.Declarations(Stylesheet, ".moka-empty-state--compact");

		Assert.Equal("row", compact["flex-direction"]);
		Assert.Equal("0.875rem 1rem", compact["padding"]);
		Assert.Equal("left", compact["text-align"]);
	}

	[Theory]
	[InlineData(".moka-empty-state--compact > .moka-empty-state__icon")]
	[InlineData(".moka-gate .moka-empty-state--compact > .moka-empty-state__icon")]
	public void CompactIcon_IsA34PixelTile(string selector)
	{
		IReadOnlyDictionary<string, string> icon = ScopedCss.Declarations(Stylesheet, selector);

		Assert.Equal("2.125rem", icon["width"]);
		Assert.Equal("2.125rem", icon["height"]);
		Assert.Equal("1rem", icon["--_empty-icon"]);
	}

	[Fact]
	public void GateIcon_IsA44PixelTile()
	{
		IReadOnlyDictionary<string, string> icon = ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-empty-state__icon");

		Assert.Equal("2.75rem", icon["width"]);
		Assert.Equal("2.75rem", icon["height"]);
		Assert.Equal("var(--moka-radius-lg)", icon["border-radius"]);
		Assert.Equal("var(--moka-color-surface-variant)", icon["background"]);
		Assert.Equal("var(--moka-color-icon)", icon["color"]);
		Assert.Equal("1.25rem", icon["--_empty-icon"]);
	}

	[Fact]
	public void GateTitle_Is15Pixels()
	{
		Assert.Equal("calc(0.9375rem * var(--moka-font-scale))",
			ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-empty-state__title")["font-size"]);
	}

	[GeneratedRegex(@" b-\w+(="""")?")]
	private static partial Regex ScopeAttribute();
}
