using System.Text.RegularExpressions;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// An app that wanted its pages in an inset panel couldn't draw one: a wrapper inside main breaks moka-fill-main, which
// only matches a page root sitting straight in main. Framed styles main itself; the page on the background stays the
// default.
public partial class MokaAppShellFramedTests : BunitContext
{
	private const string Css = "MokaAppShell.razor.css";
	private const string Main = ".moka-app-shell--framed > .moka-app-shell__main";

	private static readonly MokaNavEntry[] Entries = [new(string.Empty, "Home", "/", MokaIcons.Navigation.Home)];

	public MokaAppShellFramedTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Navigation/AppShell/MokaAppShell.razor.js");
		module.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void TheMainArea_IsNotFramedByDefault()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(framed: false);

		Assert.Equal("moka-app-shell", cut.Find(".moka-app-shell").ClassName);
	}

	[Fact]
	public void Framed_IsAClassOnTheRoot_AndThePageStaysStraightInMain()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(framed: true);

		Assert.Equal("moka-app-shell moka-app-shell--framed", cut.Find(".moka-app-shell").ClassName);
		Assert.NotNull(cut.Find(".moka-app-shell > main.moka-app-shell__main > .page.moka-fill-main"));
	}

	[Fact]
	public void TheFrame_IsAnInsetPanelOfTokens()
	{
		IReadOnlyDictionary<string, string> main = ScopedCss.Declarations(Css, Main);

		Assert.Equal("var(--moka-page-gap)", main["margin"]);
		Assert.Equal("var(--moka-border-width) solid var(--moka-color-outline-variant)", main["border"]);
		Assert.Equal("var(--moka-radius-card)", main["border-radius"]);
		Assert.Equal("var(--moka-color-surface)", main["background-color"]);
		Assert.False(main.ContainsKey("padding"));
		Assert.Equal("var(--moka-main-padding)", ScopedCss.Declarations(Css, ".moka-app-shell__main")["padding"]);
	}

	// The fill rule keeps its room for a floating bar; the framed one takes the panel's gap above and below off the window.
	[Fact]
	public void APageThatFillsMain_FillsThePanel()
	{
		IReadOnlyDictionary<string, string> plain = ScopedCss.Declarations(Css, ".moka-app-shell__main:has(> .moka-fill-main)");
		IReadOnlyDictionary<string, string> framed = ScopedCss.Declarations(Css, Main + ":has(> .moka-fill-main)");

		Assert.Equal("calc(100dvh - var(--moka-appbar-space, 0px))", plain["height"]);
		Assert.Equal("calc(var(--moka-main-padding-top) + var(--moka-action-bar-space, 0px))", plain["padding-bottom"]);
		Assert.Equal("calc(100dvh - var(--moka-appbar-space, 0px) - 2 * var(--moka-page-gap))", framed["height"]);
	}

	// Below the phone breakpoint the frame goes: every framed rule that draws it sits in the wide media query. The new
	// design's rule only takes a margin away, which a phone's main doesn't have.
	[Fact]
	public void TheFrame_IsOnlyForWideWindows()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css)).ReplaceLineEndings("\n");

		foreach (Match rule in FramedRule().Matches(css).Where(rule => !Comment().Replace(rule.Value, "").Trim().StartsWith(".moka-gate", StringComparison.Ordinal)))
		{
			string before = css[..rule.Index];
			int wide = before.LastIndexOf("@media (min-width: 721px) {", StringComparison.Ordinal);
			int narrow = before.LastIndexOf("@media (max-width: 720px) {", StringComparison.Ordinal);
			Assert.True(wide > narrow, $"Outside the wide query: {rule.Value}");
			Assert.Equal(-1, before[wide..].IndexOf("\n}", StringComparison.Ordinal));
		}
	}

	// The menu needs no line of its own beside the panel, and on the new design its padding is the gap.
	[Fact]
	public void TheMenusLine_Goes_AndTheNewDesignDropsTheLeftGap()
	{
		Assert.Equal("transparent",
			ScopedCss.Declarations(Css, ".moka-app-shell.moka-app-shell--framed ::deep .moka-sidebar.moka-app-shell__nav")["border-right-color"]);
		Assert.Equal(["margin-left"], ScopedCss.Declarations(Css, ".moka-gate " + Main).Keys);
		Assert.Equal("0", ScopedCss.Declarations(Css, ".moka-gate " + Main)["margin-left"]);
	}

	private IRenderedComponent<MokaAppShell> Shell(bool framed)
	{
		SetRendererInfo(new RendererInfo("Server", true));
		return Render<MokaAppShell>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.Framed, framed)
			.Add(x => x.ChildContent, "<div class=\"page moka-fill-main\"></div>"));
	}

	[GeneratedRegex(@"[^{}]*moka-app-shell--framed[^{}]*\{")]
	private static partial Regex FramedRule();

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();
}
