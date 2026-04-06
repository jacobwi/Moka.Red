using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.Button;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaButtonTests : BunitContext
{
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
	public void Loading_DisablesAndShowsSpinner()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Loading, true)
			.AddChildContent("Test"));

		IElement button = cut.Find("button");
		Assert.True(button.HasAttribute("disabled"));
		Assert.Contains("moka-btn--loading", button.ClassName, StringComparison.Ordinal);

		IElement spinner = cut.Find(".moka-btn-spinner");
		Assert.NotNull(spinner);
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
}
