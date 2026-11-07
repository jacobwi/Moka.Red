using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Primitives.SplitButton;
using Moka.Red.Primitives.ThemeSwitcher;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// A menu drawn inside its button's wrapper was cut off by a card's overflow and painted under any later element
// with a stacking context of its own. The menus now go to the top layer through Core's moka-popup.js; what that
// does in the browser can't run here, so these tests cover what the components render for it and when they call it.
public class TopLayerPanelTests : BunitContext
{
	private static readonly RenderFragment Items = builder =>
	{
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "role", "menuitem");
		builder.AddContent(2, "Save as");
		builder.CloseElement();
	};

	private static readonly IReadOnlyList<MokaThemeSwitcherItem> Themes =
	[
		new("Dark", MokaTheme.Dark),
		new("Light", MokaTheme.Light)
	];

	private readonly BunitJSModuleInterop _popup;

	public TopLayerPanelTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_popup = TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public async Task SplitButton_OpensItsMenuUnderTheWholeButton_AtLeastAsWideAsIt()
	{
		IRenderedComponent<MokaSplitButton> cut = RenderSplitButton();
		string? rootId = cut.Find(".moka-split-btn").GetAttribute("blazor:elementReference");

		await cut.Find(".moka-split-btn__toggle").ClickAsync(new MouseEventArgs());

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-split-btn__dropdown"), rootId, "bottom-end");
		Assert.Equal("min", options["width"]);
		Assert.Null(_popup.Invocations["open"][^1].Arguments[3]);
	}

	[Fact]
	public async Task SplitButton_ClosesItsMenu_WhenAnItemIsPicked()
	{
		IRenderedComponent<MokaSplitButton> cut = RenderSplitButton();
		await cut.Find(".moka-split-btn__toggle").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-split-btn__dropdown").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-split-btn__dropdown"));
		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task SplitButton_ClosesItsMenu_WhenDisposedOpen()
	{
		IRenderedComponent<MokaSplitButton> cut = RenderSplitButton();
		await cut.Find(".moka-split-btn__toggle").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task SplitButton_WithoutAMenu_CallsNothing()
	{
		IRenderedComponent<MokaSplitButton> cut = Render<MokaSplitButton>(p => p.AddChildContent("Save"));

		await cut.Find(".moka-split-btn__toggle").ClickAsync(new MouseEventArgs());

		Assert.Equal(0, TopLayerPopup.Count(_popup, "open"));
	}

	[Fact]
	public async Task ThemeSwitcher_OpensItsMenuUnderTheSwitcher_AsWideAsIt()
	{
		IRenderedComponent<MokaThemeSwitcher> cut = Render<MokaThemeSwitcher>(p => p.Add(x => x.Themes, Themes));
		string? rootId = cut.Find(".moka-theme-switcher").GetAttribute("blazor:elementReference");

		await cut.Find(".moka-theme-switcher__trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-theme-switcher__dropdown"), rootId, "bottom-start");
		Assert.Equal("match", options["width"]);
	}

	[Fact]
	public async Task ThemeSwitcher_ClosesItsMenu_WhenAThemeIsPicked_OrWhenDisposedOpen()
	{
		IRenderedComponent<MokaThemeSwitcher> cut = Render<MokaThemeSwitcher>(p => p.Add(x => x.Themes, Themes));
		await cut.Find(".moka-theme-switcher__trigger").ClickAsync(new MouseEventArgs());

		await cut.FindAll("[role=menuitemradio]")[1].ClickAsync(new MouseEventArgs());
		TopLayerPopup.AssertClosed(_popup);
		Assert.Equal(1, TopLayerPopup.Count(_popup, "close"));

		await cut.Find(".moka-theme-switcher__trigger").ClickAsync(new MouseEventArgs());
		await DisposeComponentsAsync();

		Assert.Equal(2, TopLayerPopup.Count(_popup, "close"));
	}

	private IRenderedComponent<MokaSplitButton> RenderSplitButton() =>
		Render<MokaSplitButton>(p => p
			.AddChildContent("Save")
			.Add(x => x.DropdownContent, Items));
}
