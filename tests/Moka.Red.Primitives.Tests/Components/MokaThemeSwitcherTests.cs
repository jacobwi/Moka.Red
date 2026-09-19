using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Primitives.ThemeSwitcher;

namespace Moka.Red.Primitives.Tests.Components;

// The switcher is a WAI-ARIA menu button whose menu holds menuitemradio items. The arrow keys and
// focus moves live in MokaThemeSwitcher.razor.js; these tests cover what the component renders for
// them and for screen readers, and when it calls the script.
public class MokaThemeSwitcherTests : BunitContext
{
	private const string SwitcherModule = "./_content/Moka.Red.Primitives/ThemeSwitcher/MokaThemeSwitcher.razor.js";

	private static readonly MokaThemeSwitcherItem[] Themes =
	[
		new("Light", MokaTheme.Light, "Default light palette"),
		new("Dark", MokaTheme.Dark),
		new("Ocean", MokaTheme.Light.WithPrimary("#0277bd"), "Light with a blue primary")
	];

	public MokaThemeSwitcherTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task TheButton_OpensAMenuAndSaysSo()
	{
		IRenderedComponent<MokaThemeSwitcher> cut = RenderSwitcher(MokaTheme.Light);

		IElement trigger = cut.Find(".moka-theme-switcher__trigger");
		Assert.Equal("button", trigger.GetAttribute("type"));
		Assert.Equal("menu", trigger.GetAttribute("aria-haspopup"));
		Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
		Assert.False(trigger.HasAttribute("aria-controls"));
		Assert.Equal("Theme: Light", trigger.GetAttribute("aria-label"));

		await trigger.ClickAsync(new MouseEventArgs());

		trigger = cut.Find(".moka-theme-switcher__trigger");
		IElement menu = cut.Find("[role=menu]");
		Assert.Equal("true", trigger.GetAttribute("aria-expanded"));
		Assert.Equal(menu.Id, trigger.GetAttribute("aria-controls"));
		Assert.Equal(trigger.Id, menu.GetAttribute("aria-labelledby"));
	}

	[Fact]
	public async Task Themes_AreRadioMenuItems_CheckedAsStrings()
	{
		IRenderedComponent<MokaThemeSwitcher> cut = RenderSwitcher(MokaTheme.Dark);

		await cut.Find(".moka-theme-switcher__trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyList<IElement> items = cut.FindAll("[role=menu] > [role=menuitemradio]");
		Assert.Equal(["Light", "Dark", "Ocean"], items.Select(i => i.GetAttribute("aria-label")));
		Assert.Equal(["false", "true", "false"], items.Select(i => i.GetAttribute("aria-checked")));
		Assert.All(items, item => Assert.Equal("-1", item.GetAttribute("tabindex")));

		// The description is read after the name instead of running into it.
		string? describedBy = items[0].GetAttribute("aria-describedby");
		Assert.Equal("Default light palette", cut.Find($"#{describedBy}").TextContent);
		Assert.False(items[1].HasAttribute("aria-describedby"));
	}

	[Fact]
	public async Task BindsTheMenuKeys_AndFocusesTheCheckedThemeOnOpen()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(SwitcherModule);

		IRenderedComponent<MokaThemeSwitcher> cut = RenderSwitcher(MokaTheme.Light);
		string? rootId = cut.Find(".moka-theme-switcher").GetAttribute("blazor:elementReference");

		ElementReference bound = Assert.IsType<ElementReference>(module.VerifyInvoke("bindSwitcher").Arguments[0]);
		Assert.Equal(rootId, bound.Id);
		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "focusCheckedItem");

		await cut.Find(".moka-theme-switcher__trigger").ClickAsync(new MouseEventArgs());

		ElementReference focused = Assert.IsType<ElementReference>(module.VerifyInvoke("focusCheckedItem").Arguments[0]);
		Assert.Equal(rootId, focused.Id);
	}

	[Fact]
	public async Task PickingATheme_ClosesTheMenuAndRenamesTheButton()
	{
		List<MokaTheme?> picks = [];
		IRenderedComponent<MokaThemeSwitcher> cut = Render<MokaThemeSwitcher>(p => p
			.Add(x => x.Themes, Themes)
			.Add(x => x.SelectedTheme, MokaTheme.Light)
			.Add(x => x.SelectedThemeChanged, theme => picks.Add(theme)));

		await cut.Find(".moka-theme-switcher__trigger").ClickAsync(new MouseEventArgs());
		await cut.FindAll("[role=menuitemradio]")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal([MokaTheme.Dark], picks);
		Assert.Empty(cut.FindAll("[role=menu]"));
		IElement trigger = cut.Find(".moka-theme-switcher__trigger");
		Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
		Assert.Equal("Theme: Dark", trigger.GetAttribute("aria-label"));
	}

	// The pick used to write the SelectedTheme parameter, so the parent's next render put its own
	// theme back when it did not bind SelectedTheme. MokaTheme.Light is a new, equal instance each time.
	[Fact]
	public async Task AParentRerender_WithTheSameTheme_KeepsTheUsersPick()
	{
		IRenderedComponent<MokaThemeSwitcher> cut = RenderSwitcher(MokaTheme.Light);

		await cut.Find(".moka-theme-switcher__trigger").ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-theme-switcher__item")[1].ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.SelectedTheme, MokaTheme.Light));

		Assert.Equal("Dark", cut.Find(".moka-theme-switcher__label").TextContent);

		// A theme the parent has not passed before still wins.
		cut.Render(p => p.Add(x => x.SelectedTheme, Themes[2].Theme));

		Assert.Equal("Ocean", cut.Find(".moka-theme-switcher__label").TextContent);
	}

	private IRenderedComponent<MokaThemeSwitcher> RenderSwitcher(MokaTheme selected) =>
		Render<MokaThemeSwitcher>(p => p
			.Add(x => x.Themes, Themes)
			.Add(x => x.SelectedTheme, selected));
}
