using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// A tooltip's own Look, Arrow, Motion and Size are the theme's classes written on its popup, so they win over the
// root's, and its rich content (an icon or a dot, a title, the text and key caps) renders in that order.
public sealed partial class MokaTooltipLookTests : BunitContext
{
	private const string Css = "MokaTooltip.razor.css";
	private const string GateMarker = "/* Gate look.";

	[Fact]
	public void APlainTooltip_RendersAsItAlwaysHas()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Text, "Save"));

		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.Equal(["moka-tooltip-popup", "moka-tooltip-popup--top"], popup.ClassList);
		Assert.Equal("Save", popup.TextContent.Trim());
		Assert.Empty(popup.Children);
	}

	[Theory]
	[InlineData(MokaTooltipLook.Standard, "moka-tooltips-standard")]
	[InlineData(MokaTooltipLook.Flat, "moka-tooltips-flat")]
	[InlineData(MokaTooltipLook.Elevated, "moka-tooltips-elevated")]
	[InlineData(MokaTooltipLook.Glass, "moka-tooltips-glass")]
	[InlineData(MokaTooltipLook.AccentTint, "moka-tooltips-accent-tint")]
	[InlineData(MokaTooltipLook.Contrast, "moka-tooltips-contrast")]
	[InlineData(MokaTooltipLook.Terminal, "moka-tooltips-terminal")]
	public void ALook_IsAClassOnThePopup(MokaTooltipLook look, string expected)
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Text, "Save").Add(x => x.Look, look));

		Assert.Equal([expected], TooltipClasses(cut.Find(".moka-tooltip-popup")));
		Assert.Empty(TooltipClasses(cut.Find(".moka-tooltip-trigger")));
	}

	[Theory]
	[InlineData(true, "moka-tooltips-arrow")]
	[InlineData(false, "moka-tooltips-no-arrow")]
	public void Arrow_IsAClassEitherWay(bool arrow, string expected)
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Text, "Save").Add(x => x.Arrow, arrow));

		Assert.Equal([expected], TooltipClasses(cut.Find(".moka-tooltip-popup")));
	}

	[Theory]
	[InlineData(MokaTooltipMotion.Fade, "moka-tooltips-motion-fade")]
	[InlineData(MokaTooltipMotion.Slide, "moka-tooltips-motion-slide")]
	[InlineData(MokaTooltipMotion.Grow, "moka-tooltips-motion-grow")]
	[InlineData(MokaTooltipMotion.Instant, "moka-tooltips-motion-instant")]
	public void Motion_IsAClassOnThePopup(MokaTooltipMotion motion, string expected)
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Text, "Save").Add(x => x.Motion, motion));

		Assert.Equal([expected], TooltipClasses(cut.Find(".moka-tooltip-popup")));
	}

	[Theory]
	[InlineData(MokaTooltipSize.Compact, "moka-tooltips-size-compact")]
	[InlineData(MokaTooltipSize.Standard, "moka-tooltips-size-standard")]
	[InlineData(MokaTooltipSize.Roomy, "moka-tooltips-size-roomy")]
	public void Size_IsAClassOnThePopup(MokaTooltipSize size, string expected)
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Text, "Save").Add(x => x.Size, size));

		Assert.Equal([expected], TooltipClasses(cut.Find(".moka-tooltip-popup")));
	}

	// The theme's classes sit on the root and only set variables; the tooltip's sit on the popup, the nearest element
	// to declare them, so they are what the popup reads.
	[Fact]
	public void ATooltipsOwnChoices_SitCloserThanTheThemes()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red.WithTooltips(MokaTooltipLook.Glass).WithTooltipArrows(true).WithTooltipMotion(MokaTooltipMotion.Grow))
			.AddChildContent<MokaTooltip>(tooltip => tooltip
				.Add(x => x.Text, "Save")
				.Add(x => x.Look, MokaTooltipLook.Terminal)
				.Add(x => x.Arrow, false)
				.Add(x => x.Motion, MokaTooltipMotion.Instant)));

		Assert.Equal(["moka-tooltips-glass", "moka-tooltips-arrow", "moka-tooltips-motion-grow"], TooltipClasses(cut.Find(".moka-root")));
		Assert.Equal(["moka-tooltips-terminal", "moka-tooltips-no-arrow", "moka-tooltips-motion-instant"], TooltipClasses(cut.Find(".moka-tooltip-popup")));
	}

	[Fact]
	public void WithoutChoicesOfItsOwn_ItLeavesThemToTheTheme()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red.WithTooltips(MokaTooltipLook.Glass))
			.AddChildContent<MokaTooltip>(tooltip => tooltip.Add(x => x.Text, "Save")));

		Assert.Empty(TooltipClasses(cut.Find(".moka-tooltip-popup")));
	}

	[Fact]
	public void RichContent_IsIconTitleTextThenKeys()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Writes the draft to disk")
			.Add(x => x.Title, "Save")
			.Add(x => x.Icon, MokaIcons.Action.Save)
			.Add(x => x.Shortcut, "Ctrl+S"));

		IElement rich = cut.Find(".moka-tooltip-popup > .moka-tooltip-rich");
		Assert.Equal(["svg", "span", "span"], rich.Children.Select(child => child.LocalName));
		Assert.Contains("moka-tooltip-icon", rich.Children[0].ClassList);
		Assert.Equal("true", rich.Children[0].GetAttribute("aria-hidden"));
		Assert.Equal(["moka-tooltip-title", "moka-tooltip-text"], rich.Children[1].Children.Select(child => child.ClassName));
		Assert.Equal("Save", rich.Children[1].Children[0].TextContent);
		Assert.Equal("Writes the draft to disk", rich.Children[1].Children[1].TextContent);
		Assert.Equal("moka-tooltip-keys", rich.Children[2].ClassName);
	}

	// One key cap per key, through ShortcutKeys, as every other shortcut hint in the library.
	[Fact]
	public void TheShortcut_IsAKeyCapPerKey()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Text, "Command palette").Add(x => x.Shortcut, "Ctrl + Shift + P"));

		Assert.Equal(["Ctrl", "Shift", "P"], cut.FindAll(".moka-tooltip-keys > kbd").Select(key => key.TextContent));
		Assert.All(cut.FindAll(".moka-tooltip-keys > kbd"), key => Assert.Contains("moka-kbd", key.ClassList));
	}

	[Fact]
	public void AShortcutAlone_StillShowsAPopup()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Shortcut, "Ctrl++"));

		Assert.Equal(["Ctrl", "+"], cut.FindAll(".moka-tooltip-popup kbd").Select(key => key.TextContent));
		Assert.Empty(cut.FindAll(".moka-tooltip-body"));
	}

	[Fact]
	public void TheShortcut_FollowsAChange()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Text, "Find").Add(x => x.Shortcut, "Ctrl+F"));

		cut.Render(p => p.Add(x => x.Shortcut, "Ctrl+Shift+F"));

		Assert.Equal(["Ctrl", "Shift", "F"], cut.FindAll(".moka-tooltip-keys > kbd").Select(key => key.TextContent));
	}

	[Fact]
	public void ADot_IsAGlowingStatusDotBeforeTheText()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Text, "web01 is online").Add(x => x.Dot, MokaColor.Success));

		IElement dot = cut.Find(".moka-tooltip-rich > .moka-status-dot");
		Assert.Contains("moka-tooltip-dot", dot.ClassList);
		Assert.Contains("moka-status-dot--success", dot.ClassList);
		Assert.Contains("moka-status-dot--glow", dot.ClassList);
		Assert.Contains("moka-tooltip-dot", cut.Find(".moka-tooltip-rich").FirstElementChild!.ClassList);
	}

	[Fact]
	public void AnIcon_WinsOverADot()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Synced")
			.Add(x => x.Icon, MokaIcons.Status.CheckCircle)
			.Add(x => x.Dot, MokaColor.Success));

		Assert.Single(cut.FindAll(".moka-tooltip-rich > svg.moka-tooltip-icon"));
		Assert.Empty(cut.FindAll(".moka-status-dot"));
	}

	[Fact]
	public void AnIconWithNothingToSay_ShowsNoPopup()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Icon, MokaIcons.Status.CheckCircle));

		Assert.Empty(cut.FindAll(".moka-tooltip-popup"));
	}

	[Fact]
	public void Content_WinsOverEveryRichPart()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Title, "Save")
			.Add(x => x.Shortcut, "Ctrl+S")
			.Add(x => x.Dot, MokaColor.Info)
			.Add(x => x.Content, "<b>Mine</b>"));

		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.Equal("Mine", popup.TextContent.Trim());
		Assert.Empty(cut.FindAll(".moka-tooltip-rich"));
		Assert.DoesNotContain("moka-tooltip-popup--wrap", popup.ClassList);
	}

	// A title is a sentence's heading, so the tooltip wraps, at 20rem from the stylesheet unless MaxWidth says.
	[Fact]
	public void ATitle_Wraps_WithoutAMaxWidth()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Title, "Save").Add(x => x.Text, "Writes the draft"));

		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.Contains("moka-tooltip-popup--wrap", popup.ClassList);
		Assert.DoesNotContain("--moka-tooltip-max-width", popup.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("min(var(--moka-tooltip-max-width, 20rem)", ScopedCss.Declarations(Css, ".moka-tooltip-popup--wrap")["max-width"], StringComparison.Ordinal);
	}

	[Fact]
	public void Mono_IsAClass()
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p.Add(x => x.Text, "10.0.0.1").Add(x => x.Mono, true));

		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.Contains("moka-tooltip-popup--mono", popup.ClassList);
		Assert.Equal("10.0.0.1", popup.TextContent.Trim());
		Assert.Equal("var(--moka-font-family-mono)", ScopedCss.Declarations(Css, ".moka-tooltip-popup--mono")["font-family"]);
		Assert.Equal("tabular-nums", ScopedCss.Declarations(Css, ".moka-tooltip-popup--mono")["font-variant-numeric"]);
		Assert.Equal("var(--moka-font-family-mono)", ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup--mono")["font-family"]);
	}

	// Each value reads an app's own variable first, the look's next, and what it drew before last, so with no look and
	// no variable the tooltip is today's.
	[Fact]
	public void TheGateTooltip_ReadsTheLook_BehindTheAppsVariables()
	{
		IReadOnlyDictionary<string, string> popup = ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup");

		Assert.Equal("var(--moka-tooltip-bg, var(--moka-tooltip-look-bg, var(--moka-color-surface-variant)))", popup["--_tt-bg"]);
		Assert.Equal("var(--moka-tooltip-border, var(--moka-tooltip-look-border, var(--moka-color-outline-strong)))", popup["--_tt-border"]);
		Assert.Equal("var(--_tt-bg)", popup["background"]);
		Assert.Equal("var(--_tt-border)", popup["border-color"]);
		Assert.Equal("var(--moka-tooltip-color, var(--moka-tooltip-look-color, var(--moka-color-on-surface)))", popup["color"]);
		Assert.Equal("var(--moka-tooltip-look-radius, var(--moka-radius-md))", popup["--_tt-radius"]);
		Assert.Equal("var(--moka-tooltip-look-shadow, var(--moka-shadow-popup))", popup["box-shadow"]);
		Assert.Equal("var(--moka-tooltip-size-padding, var(--moka-spacing-sm) var(--moka-button-padding-sm))", popup["padding"]);
		Assert.Equal("var(--moka-tooltip-size-font, var(--moka-font-size-sm))", popup["font-size"]);
		Assert.Equal("var(--moka-tooltip-look-weight, var(--moka-font-weight-semibold))", popup["font-weight"]);
		Assert.Equal("var(--moka-tooltip-look-font, inherit)", popup["font-family"]);
		Assert.Equal("var(--moka-tooltip-motion-time, 0.15s)", popup["transition-duration"]);
		Assert.Equal("opacity, display, scale, var(--moka-tooltip-motion-margin, opacity)", popup["transition-property"]);
		Assert.Equal("allow-discrete", popup["transition-behavior"]);
	}

	// A backdrop filter on the tooltip would make it the containing block of its fixed arrow, which then couldn't find
	// the trigger, so Glass blurs from a layer under the text.
	[Fact]
	public void Glass_BlursFromALayer_NotFromTheTooltip()
	{
		Assert.False(ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup").ContainsKey("backdrop-filter"));

		IReadOnlyDictionary<string, string> layer = ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup::after");
		Assert.Equal("var(--moka-tooltip-look-blur, none)", layer["backdrop-filter"]);
		Assert.Equal("-1", layer["z-index"]);
	}

	[Fact]
	public void TheArrow_IsOffUnlessAClassTurnsItOn()
	{
		IReadOnlyDictionary<string, string> arrow = ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup::before");

		Assert.Equal("var(--moka-tooltip-arrow-display, none)", ScopedCss.Values(Css, ".moka-gate .moka-tooltip-popup::before", "display")[0]);
		Assert.Equal("var(--_tt-bg)", arrow["background"]);
		Assert.Equal("0 solid var(--_tt-edge)", arrow["border"]);
		Assert.Equal("45deg", arrow["rotate"]);
	}

	// Without anchored queries the browser may move the tooltip without saying where, so the arrow hides rather than point
	// the wrong way; with them, every fallback a placement lists has a rule.
	[Fact]
	public void TheArrow_FollowsEveryFallback_OrHides()
	{
		string css = CssText();

		Assert.Contains("@supports (anchor-name: --moka-tooltip) and (not (container-type: anchored)) {\n\t.moka-gate .moka-tooltip-popup::before {\n\t\tdisplay: none;", css, StringComparison.Ordinal);
		Assert.Contains("@supports (container-type: anchored) {\n\t.moka-gate .moka-tooltip-popup {\n\t\tcontainer-type: anchored;", css, StringComparison.Ordinal);

		foreach (string fallback in ScopedCss.Declarations(Css, ".moka-tooltip-popup--top")["position-try-fallbacks"].Split(',')
			         .Concat(ScopedCss.Declarations(Css, ".moka-tooltip-popup--bottom")["position-try-fallbacks"].Split(','))
			         .Concat(ScopedCss.Declarations(Css, ".moka-tooltip-popup--left")["position-try-fallbacks"].Split(','))
			         .Select(option => option.Trim()))
		{
			Assert.Contains($"anchored(fallback: {fallback})", css, StringComparison.Ordinal);
		}
	}

	[Theory]
	[InlineData("anchored(fallback: top) or", "0 1px 1px 0", "var(--_tt-below)")]
	[InlineData("anchored(fallback: bottom) or", "1px 0 0 1px", "var(--_tt-above)")]
	public void AnUpOrDownFallback_PointsTheArrowAtTheTrigger(string query, string borders, string top)
	{
		string block = ContainerBlock(query);

		Assert.Contains($"border-width: {borders};", block, StringComparison.Ordinal);
		Assert.Contains($"top: {top};", block, StringComparison.Ordinal);
		Assert.Contains("left: var(--_tt-across-x);", block, StringComparison.Ordinal);
	}

	[Fact]
	public void FlipInline_MovesASideArrowToTheOtherSide()
	{
		string block = ContainerBlock("anchored(fallback: flip-inline)");

		Assert.Contains(".moka-gate .moka-tooltip-popup--left::before {\n\t\t\ttop: var(--_tt-across-y);\n\t\t\tleft: var(--_tt-before);", block, StringComparison.Ordinal);
		Assert.Contains(".moka-gate .moka-tooltip-popup--right::before {\n\t\t\ttop: var(--_tt-across-y);\n\t\t\tleft: var(--_tt-after);", block, StringComparison.Ordinal);
	}

	// Across its side the arrow points at the trigger's middle, read from the trigger's anchor, and stays clear of the
	// tooltip's corners, read from the tooltip's own anchor, which is scoped to it.
	[Fact]
	public void TheAnchoredArrow_PointsAtTheTriggersMiddle_InsideTheCorners()
	{
		string css = CssText();

		Assert.Contains("anchor-name: --moka-tooltip-box;\n\t\tanchor-scope: --moka-tooltip-box;", css, StringComparison.Ordinal);
		Assert.Contains("position-anchor: inherit;", css, StringComparison.Ordinal);
		Assert.Contains("--_tt-across-x: clamp(\n\t\t\tcalc(anchor(--moka-tooltip-box left, -1px) + var(--_tt-inset) - var(--_tt-arrow) / 2),\n\t\t\tcalc(anchor(center, 50%) - var(--_tt-arrow) / 2),", css, StringComparison.Ordinal);
		Assert.Equal("calc(var(--_tt-radius) + var(--_tt-arrow) * 0.71)", ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup")["--_tt-inset"]);
	}

	[Fact]
	public void WithoutAnchorPositioning_TheEdgePlacementsPointNearTheirEnd()
	{
		Assert.Contains("calc(var(--_tt-inset) - var(--_tt-arrow) / 2)", ScopedCss.Values(Css, ".moka-gate .moka-tooltip-popup--top-start::before", "left"));
		Assert.Contains("calc(100% - var(--_tt-inset) - var(--_tt-arrow) / 2)", ScopedCss.Values(Css, ".moka-gate .moka-tooltip-popup--bottom-end::before", "left"));
		Assert.Equal("var(--_tt-across-x)", ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup--top-start::before")["left"]);
	}

	// Slide grows the margin, so it moves away from whichever side the tooltip lands on; Grow scales from the trigger's
	// side. Both start where the theme's motion class says.
	[Fact]
	public void Motion_StartsFromTheVariables()
	{
		string css = CssText();
		int starting = css.IndexOf("@starting-style {\n\t.moka-gate .moka-tooltip-trigger:hover > .moka-tooltip-popup,", StringComparison.Ordinal);

		Assert.True(starting > 0);
		string block = css[starting..css.IndexOf("\n}\n", starting, StringComparison.Ordinal)];
		Assert.Contains("scale: var(--moka-tooltip-motion-scale, none);", block, StringComparison.Ordinal);
		Assert.Contains(
			":is(.moka-tooltip-popup--top, .moka-tooltip-popup--top-start, .moka-tooltip-popup--top-end, .moka-tooltip-popup--bottom, .moka-tooltip-popup--bottom-start, .moka-tooltip-popup--bottom-end) {\n\t\tmargin-block: var(--moka-tooltip-motion-from, var(--moka-spacing-xs));",
			block, StringComparison.Ordinal);
		Assert.Contains(":is(.moka-tooltip-popup--left, .moka-tooltip-popup--right) {\n\t\tmargin-inline: var(--moka-tooltip-motion-from, var(--moka-spacing-xs));", block, StringComparison.Ordinal);

		Assert.Equal("bottom", ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup--top")["transform-origin"]);
		Assert.Equal("left top", ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup--bottom-start")["transform-origin"]);
		Assert.Equal("right", ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-popup--left")["transform-origin"]);
	}

	// The rules for rich content apply on both designs, so they read tokens alone, like the Gate block.
	[Fact]
	public void TheRichContentRules_ReadTokens()
	{
		string css = CssText();
		int start = css.IndexOf("/* Rich content", StringComparison.Ordinal);
		int end = css.IndexOf(GateMarker, StringComparison.Ordinal);
		Assert.True(start > 0 && end > start);

		string rules = Comment().Replace(css[start..end], " ");
		Assert.Empty(HardCodedColor().Matches(rules));
		Assert.Empty(LargePixelSize().Matches(rules));
	}

	[Fact]
	public void KeyCaps_TakeTheTooltipsColors_OnTheNewDesign()
	{
		IReadOnlyDictionary<string, string> key = ScopedCss.Declarations(Css, ".moka-gate .moka-tooltip-keys ::deep kbd");

		Assert.Equal("inherit", key["color"]);
		Assert.Equal("transparent", key["background"]);
	}

	private static string[] TooltipClasses(IElement element) =>
		[.. element.ClassList.Where(c => c.StartsWith("moka-tooltips", StringComparison.Ordinal))];

	private static string CssText() =>
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css)).ReplaceLineEndings("\n");

	private static string ContainerBlock(string query)
	{
		string css = CssText();
		int start = css.IndexOf($"@container {query}", StringComparison.Ordinal);
		Assert.True(start > 0, $"No @container {query}");
		return css[start..css.IndexOf("\n\t}\n", start, StringComparison.Ordinal)];
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@":[^;{}]*?(#[0-9a-fA-F]{3,8}\b|rgba?\()")]
	private static partial Regex HardCodedColor();

	[GeneratedRegex(@"(?<![\d.])([3-9]|\d{2,})px\b")]
	private static partial Regex LargePixelSize();
}
