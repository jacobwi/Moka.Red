using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.Button;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaButtonTests : BunitContext
{
	private const string ButtonCss = "MokaButton.razor.css";

	[Fact]
	public void Renders_ButtonElement_ByDefault()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.AddChildContent("Click me"));

		IElement button = cut.Find("button");
		Assert.NotNull(button);
		Assert.Equal("button", button.GetAttribute("type"));
	}

	[Fact]
	public void Renders_AnchorElement_WhenHrefSet()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Href, "https://example.com")
			.AddChildContent("Link"));

		IElement anchor = cut.Find("a");
		Assert.Equal("https://example.com", anchor.GetAttribute("href"));
		Assert.Equal("button", anchor.GetAttribute("role"));
	}

	[Fact]
	public void Applies_RootClass()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.AddChildContent("Test"));

		IElement el = cut.Find("button");
		Assert.Contains("moka-btn", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Applies_VariantClass()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Variant, MokaVariant.Outlined)
			.AddChildContent("Test"));

		IElement el = cut.Find("button");
		Assert.Contains("moka-btn--outlined", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Applies_ColorClass()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Color, MokaColor.Error)
			.AddChildContent("Test"));

		IElement el = cut.Find("button");
		Assert.Contains("moka-btn--error", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Applies_SizeClass()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Size, MokaSize.Lg)
			.AddChildContent("Test"));

		IElement el = cut.Find("button");
		Assert.Contains("moka-btn--lg", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Disabled_AddsAttribute()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Disabled, true)
			.AddChildContent("Test"));

		IElement button = cut.Find("button");
		Assert.True(button.HasAttribute("disabled"));
		Assert.Contains("moka-btn--disabled", button.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task OnClick_FiresWhenClicked()
	{
		bool clicked = false;
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.OnClick, _ => { clicked = true; })
			.AddChildContent("Test"));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		Assert.True(clicked);
	}

	[Fact]
	public async Task OnClick_DoesNotFireWhenDisabled()
	{
		bool clicked = false;
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.OnClick, _ => { clicked = true; })
			.AddChildContent("Test"));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		Assert.False(clicked);
	}

	[Fact]
	public void FullWidth_AddsClass()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.FullWidth, true)
			.AddChildContent("Test"));

		IElement el = cut.Find("button");
		Assert.Contains("moka-btn--full-width", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void IconOnly_WhenNoChildContent()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.StartIcon, MokaIcons.Action.Add));

		IElement el = cut.Find("button");
		Assert.Contains("moka-btn--icon-only", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Appends_UserClass()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Class, "my-custom")
			.AddChildContent("Test"));

		IElement el = cut.Find("button");
		Assert.Contains("my-custom", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Button_ByDefault_KeepsItsClasses()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p.AddChildContent("Save"));

		Assert.Equal("moka-btn moka-btn--filled moka-btn--primary moka-btn--md", cut.Find("button").ClassName);
	}

	[Fact]
	public void IconOnly_WithoutQuiet_KeepsItsVariantAndSize()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p.Add(x => x.StartIcon, MokaIcons.Action.Add));

		Assert.Equal("moka-btn moka-btn--filled moka-btn--primary moka-btn--md moka-btn--icon-only",
			cut.Find("button").ClassName);
	}

	// Every color rule also names a variant, so without a variant class none of them reach the button.
	[Fact]
	public void Quiet_ReplacesTheVariantAndSizeClasses()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Quiet, true)
			.Add(x => x.Variant, MokaVariant.Outlined)
			.Add(x => x.Size, MokaSize.Lg)
			.Add(x => x.StartIcon, MokaIcons.Action.Add));

		Assert.Equal("moka-btn moka-btn--primary moka-btn--icon-only moka-btn--quiet", cut.Find("button").ClassName);
	}

	[Fact]
	public void Quiet_WithError_KeepsTheColorClass()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Quiet, true)
			.Add(x => x.Color, MokaColor.Error)
			.Add(x => x.StartIcon, MokaIcons.Action.Delete));

		Assert.Equal("moka-btn moka-btn--error moka-btn--icon-only moka-btn--quiet", cut.Find("button").ClassName);
	}

	[Fact]
	public void Quiet_DrawsADefaultIconAtSixteenPixels()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Quiet, true)
			.Add(x => x.StartIcon, MokaIcons.Action.Add));

		Assert.Contains("moka-icon--sm", cut.Find(".moka-btn-icon").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Quiet_PassesItsNameToTheButton()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Quiet, true)
			.Add(x => x.StartIcon, MokaIcons.Action.Add)
			.AddUnmatched("aria-label", "Add"));

		Assert.Equal("Add", cut.Find("button").GetAttribute("aria-label"));
	}

	[Fact]
	public void Quiet_ReadsTheApprovedTokens()
	{
		IReadOnlyDictionary<string, string> css = ScopedCss.Declarations(ButtonCss, ".moka-btn--quiet");

		Assert.Equal("0", css["padding"]);
		Assert.Equal("none", css["border"]);
		Assert.Equal("transparent", css["background-color"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", css["color"]);
		Assert.Equal("var(--moka-radius-md)", ScopedCss.Declarations(ButtonCss, ".moka-btn")["border-radius"]);
	}

	// max() can't take the token: it is a unitless 0 on the original design, and max(0, 2rem) is invalid.
	[Fact]
	public void Quiet_IsASquareOnBothDesigns()
	{
		IReadOnlyDictionary<string, string> original = ScopedCss.Declarations(ButtonCss, ".moka-btn--quiet");
		IReadOnlyDictionary<string, string> gate = ScopedCss.Declarations(ButtonCss, ".moka-gate .moka-btn--quiet");

		Assert.Equal("2rem", original["min-width"]);
		Assert.Equal("2rem", original["min-height"]);
		Assert.Equal("var(--moka-button-height-sm)", gate["min-width"]);
		Assert.Equal("var(--moka-button-height-sm)", gate["min-height"]);
	}

	[Fact]
	public void Quiet_BrightensOnHover_AndAnErrorTurnsRed()
	{
		IReadOnlyDictionary<string, string> hover =
			ScopedCss.Declarations(ButtonCss, ".moka-btn--quiet:hover:not(.moka-btn--disabled)");
		IReadOnlyDictionary<string, string> danger =
			ScopedCss.Declarations(ButtonCss, ".moka-btn--quiet.moka-btn--error:hover:not(.moka-btn--disabled)");

		Assert.Equal("var(--moka-color-surface-hover)", hover["background-color"]);
		Assert.Equal("var(--moka-color-on-surface)", hover["color"]);
		Assert.Equal("var(--moka-color-error-soft)", danger["color"]);
	}

	// Hidden overflow takes away a flex item's minimum width, so a row squeezed the button below its label.
	[Fact]
	public void Button_ClipsItsOverflowWithoutGivingUpItsMinimumWidth()
	{
		IReadOnlyDictionary<string, string> css = ScopedCss.Declarations(ButtonCss, ".moka-btn");

		Assert.Equal("clip", css["overflow"]);
	}
}
