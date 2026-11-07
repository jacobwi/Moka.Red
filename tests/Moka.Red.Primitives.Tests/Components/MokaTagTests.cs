using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.Tag;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaTagTests : BunitContext
{
	private const string Css = "MokaTag.razor.css";

	// Mokaterm's status bar, a nowrap flex row whose items shrink, drew "DEMO" as a column of four letters.
	[Fact]
	public void NewDesign_WrapsALongWord_WithoutLettingARowSqueezeItToOneLetter()
	{
		IReadOnlyDictionary<string, string> tag = ScopedCss.Declarations(Css, ".moka-gate .moka-tag");

		Assert.Equal("break-word", tag["overflow-wrap"]);
		Assert.DoesNotContain("word-break", tag.Keys);
	}

	[Fact]
	public void Tag_ByDefault_KeepsItsClassesAndNoStyle()
	{
		IRenderedComponent<MokaTag> cut = Render<MokaTag>(p => p.Add(x => x.Text, "Beta"));

		IElement tag = cut.Find(".moka-tag");
		Assert.Equal("moka-tag moka-tag--soft moka-tag--surface moka-tag--sm", tag.ClassName);
		Assert.Null(tag.GetAttribute("style"));
	}

	[Fact]
	public void New_ReplacesVariantColorAndSize()
	{
		IRenderedComponent<MokaTag> cut = Render<MokaTag>(p => p
			.Add(x => x.Text, "New")
			.Add(x => x.New, true)
			.Add(x => x.Variant, MokaVariant.Filled)
			.Add(x => x.Color, MokaColor.Error)
			.Add(x => x.Size, MokaSize.Lg));

		Assert.Equal("moka-tag moka-tag--new", cut.Find(".moka-tag").ClassName);
	}

	[Fact]
	public void New_LeavesPillAndTheConsumersClass()
	{
		IRenderedComponent<MokaTag> cut = Render<MokaTag>(p => p
			.Add(x => x.Text, "New")
			.Add(x => x.New, true)
			.Add(x => x.Pill, true)
			.Add(x => x.Class, "mine"));

		Assert.Equal("moka-tag moka-tag--pill moka-tag--new mine", cut.Find(".moka-tag").ClassName);
	}

	[Fact]
	public void New_DrawsItsIconAtOneSize_WhateverSizeSays()
	{
		IRenderedComponent<MokaTag> cut = Render<MokaTag>(p => p
			.Add(x => x.Text, "New")
			.Add(x => x.New, true)
			.Add(x => x.Size, MokaSize.Lg)
			.Add(x => x.Icon, MokaIcons.Action.Add));

		Assert.Contains("moka-icon--xs", cut.Find(".moka-tag-icon").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void New_ReadsTheApprovedTokens()
	{
		IReadOnlyDictionary<string, string> css = ScopedCss.Declarations(Css, ".moka-tag--new");

		Assert.Equal("0.125rem 0.4375rem", css["padding"]);
		Assert.Equal("var(--moka-color-primary-bg)", css["background-color"]);
		Assert.Equal("var(--moka-color-primary-light)", css["color"]);
		Assert.Equal("var(--moka-font-size-xxs)", css["font-size"]);
		Assert.Equal("var(--moka-font-weight-bold)", css["font-weight"]);
		Assert.Equal("0.06em", css["letter-spacing"]);
	}

	// .moka-gate .moka-tag has the specificity of a two-class rule and comes later, so it beats the plain one.
	[Fact]
	public void New_RestatesWhatTheGateTagRuleWouldOverride()
	{
		IReadOnlyDictionary<string, string> css = ScopedCss.Declarations(Css, ".moka-gate .moka-tag--new");

		Assert.Equal("var(--moka-font-weight-bold)", css["font-weight"]);
		Assert.Equal("0.06em", css["letter-spacing"]);
		Assert.Equal("var(--moka-radius-sm)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-tag--new:not(.moka-tag--pill)")["border-radius"]);
	}
}
