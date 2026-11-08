using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Typography;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaLinkTests : BunitContext
{
	private const string Css = "MokaLink.razor.css";

	[Fact]
	public void WithoutHrefOrOnClick_StillPointsToHash()
	{
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p.AddChildContent("Docs"));

		IElement link = cut.Find("a.moka-link");
		Assert.Equal("#", link.GetAttribute("href"));
		Assert.Equal("moka-link moka-link--underline-hover", link.ClassName);
		Assert.Empty(cut.FindAll("button"));
	}

	// A listener on every plain link would make every click a round trip.
	[Fact]
	public void APlainLink_HasNoClickListener()
	{
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p.Add(x => x.Href, "/docs"));

		Assert.False(cut.Find("a").HasAttribute("blazor:onclick"));
	}

	[Fact]
	public void WithOnClickAndNoHref_IsATextButton()
	{
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p
			.Add(x => x.OnClick, () => { })
			.AddChildContent("Show all"));

		IElement button = cut.Find("button");
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.Equal("moka-link moka-link--underline-hover moka-link--button", button.ClassName);
		Assert.Equal("Show all", button.TextContent.Trim());
		Assert.False(button.HasAttribute("href"));
		Assert.False(button.HasAttribute("disabled"));
		Assert.Empty(cut.FindAll("a"));
	}

	[Fact]
	public async Task TheTextButton_RaisesOnClick()
	{
		int clicks = 0;
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p
			.Add(x => x.OnClick, (MouseEventArgs _) => clicks++)
			.AddChildContent("Show all"));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, clicks);
	}

	[Fact]
	public async Task ADisabledTextButton_IsDisabled_AndRaisesNothing()
	{
		int clicks = 0;
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p
			.Add(x => x.OnClick, (MouseEventArgs _) => clicks++)
			.Add(x => x.Disabled, true)
			.AddChildContent("Show all"));

		IElement button = cut.Find("button");
		await button.ClickAsync(new MouseEventArgs());

		Assert.True(button.HasAttribute("disabled"));
		Assert.Equal(0, clicks);
	}

	[Fact]
	public void TheTextButton_KeepsUnderlineWeightColorAndTheConsumersAttributes()
	{
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p
			.Add(x => x.OnClick, () => { })
			.Add(x => x.Underline, MokaLinkUnderline.Always)
			.Add(x => x.Weight, MokaFontWeight.Semibold)
			.Add(x => x.Color, MokaColor.Error)
			.Add(x => x.Class, "mine")
			.AddUnmatched("aria-describedby", "hint"));

		IElement button = cut.Find("button");
		Assert.Equal("moka-link moka-link--underline-always moka-link--button mine", button.ClassName);
		Assert.Equal(
			"font-size: var(--moka-font-size-base); font-weight: var(--moka-font-weight-semibold); color: var(--moka-color-error)",
			button.GetAttribute("style"));
		Assert.Equal("hint", button.GetAttribute("aria-describedby"));
	}

	// External is about where a link opens, which a button doesn't do.
	[Fact]
	public void TheTextButton_DrawsNoExternalIcon()
	{
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p
			.Add(x => x.OnClick, () => { })
			.Add(x => x.External, true));

		Assert.Empty(cut.FindAll(".moka-link-external-icon"));
		Assert.False(cut.Find("button").HasAttribute("target"));
	}

	[Fact]
	public async Task WithHrefAndOnClick_StaysALink_ThatRaisesOnClick()
	{
		int clicks = 0;
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p
			.Add(x => x.Href, "/orders")
			.Add(x => x.OnClick, (MouseEventArgs _) => clicks++));

		IElement link = cut.Find("a.moka-link");
		await link.ClickAsync(new MouseEventArgs());

		Assert.Equal("/orders", link.GetAttribute("href"));
		Assert.DoesNotContain("moka-link--button", link.ClassName, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll("button"));
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void TheTextButton_DropsTheButtonsOwnBoxAndFont()
	{
		IReadOnlyDictionary<string, string> button = ScopedCss.Declarations(Css, ".moka-link--button");

		Assert.Equal("inline", button["display"]);
		Assert.Equal("0", button["padding"]);
		Assert.Equal("none", button["border"]);
		Assert.Equal("none", button["background"]);
		Assert.Equal("inherit", button["font"]);
		Assert.Equal("inherit", button["letter-spacing"]);
		Assert.Equal("inherit", button["text-transform"]);
		Assert.Equal("baseline", button["vertical-align"]);
		Assert.Equal("var(--moka-color-primary)", button["color"]);
	}

	[Fact]
	public void TheTextButton_HasAFocusRing_AndDimsWhenDisabled()
	{
		IReadOnlyDictionary<string, string> focus = ScopedCss.Declarations(Css, ".moka-link--button:focus-visible");
		IReadOnlyDictionary<string, string> disabled = ScopedCss.Declarations(Css, ".moka-link--button:disabled");

		Assert.Equal("var(--moka-focus-width) solid var(--moka-focus-color)", focus["outline"]);
		Assert.Equal("var(--moka-opacity-disabled)", disabled["opacity"]);
		Assert.Equal("not-allowed", disabled["cursor"]);
		Assert.Equal("var(--moka-color-primary-dark)",
			ScopedCss.Declarations(Css, ".moka-link--button:hover:not(:disabled)")["color"]);
	}
}
