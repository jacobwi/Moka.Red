using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// A provider that follows the OS writes both themes under prefers-color-scheme, so prerendering and a static page show the
// OS's scheme before any script runs, and the interactive render matches the prerendered one. The browser's answer moves
// only the class and the cascaded theme.
public sealed partial class AutoColorSchemeTests : BunitContext
{
	private const string ThemeModule = "./_content/Moka.Red.Core/moka-theme.js";

	private static readonly RendererInfo Static = new("Static", false);
	private static readonly RendererInfo Interactive = new("Server", true);

	private static readonly Dictionary<string, Provider> Providers = new(StringComparer.Ordinal)
	{
		["Red"] = new Provider(MokaTheme.Red),
		["RedLight"] = new Provider(MokaTheme.RedLight),
		["Gate"] = new Provider(MokaTheme.Gate),
		["Dark"] = new Provider(MokaTheme.Dark),
		["Light"] = new Provider(MokaTheme.Light),
		["RedPair"] = new Provider(MokaTheme.Default, MokaTheme.Red, MokaTheme.RedLight),
		["GatePair"] = new Provider(MokaTheme.Default, MokaTheme.Gate, MokaTheme.GateLight),
		["MatrixPair"] = new Provider(MokaTheme.Default, MokaTheme.Dark, MokaTheme.Light),
		["CrispPair"] = new Provider(MokaTheme.Default, MokaTheme.Crisp.WithAccent("#00e676"), MokaTheme.CrispLight.WithAccent("#00e676")),
		["Mixed"] = new Provider(MokaTheme.Default, MokaTheme.Gate, MokaTheme.Light)
	};

	public static TheoryData<string, string> NestedLayouts()
	{
		TheoryData<string, string> data = [];
		foreach (string outer in new[] { "RedPair", "MatrixPair", "Mixed", "Light", "Red" })
		{
			foreach (string inner in new[] { "Gate", "Dark", "Light", "RedLight", "RedPair", "GatePair", "MatrixPair", "CrispPair", "Mixed" })
			{
				data.Add(outer, inner);
			}
		}

		return data;
	}

	[Fact]
	public void TheFirstPaint_HoldsBothThemes_UnderTheirMediaQueries()
	{
		SetRendererInfo(Static);
		AnswerLater();

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.AutoDetectColorScheme, true)
			.AddChildContent("<span>Hello</span>"));

		IElement root = cut.Find(".moka-root");
		string css = Assert.Single(cut.FindAll("style")).TextContent;
		string scope = root.GetAttribute("data-moka-theme")!;
		Assert.False(root.HasAttribute("style"));
		Assert.Equal(MokaTheme.DefaultLight.ToCssVariables().Trim(), Rule(css, "light", Scoped(scope)));
		Assert.Equal(MokaTheme.Default.ToCssVariables().Trim(), Rule(css, "dark", Scoped(scope)));
		Assert.Contains($":root, {Scoped(scope)} {{", css, StringComparison.Ordinal);
		Assert.Equal("background: var(--moka-color-background); color-scheme: light;", Rule(css, "light", ":root"));
		Assert.Equal("background: var(--moka-color-background); color-scheme: dark;", Rule(css, "dark", ":root"));
	}

	// Until the browser answers, moka-dark would be a guess. moka-scheme-auto stands in for it under the dark media query.
	[Fact]
	public void TheFirstPaint_LeavesMokaDarkToTheMediaQuery()
	{
		SetRendererInfo(Static);
		AnswerLater();

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.AutoDetectColorScheme, true));

		Assert.Equal(["moka-root", "moka-scheme-auto", "moka-gate", "moka-popups-standard", "moka-buttons-standard"],
			cut.Find(".moka-root").ClassList);
	}

	[Fact]
	public void TheMatrixPair_DrawsNoPageRules_AsItsThemesDont()
	{
		SetRendererInfo(Static);
		AnswerLater();

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.AutoDetectColorScheme, true)
			.Add(x => x.DarkTheme, MokaTheme.Dark)
			.Add(x => x.LightTheme, MokaTheme.Light));

		string css = cut.Find("style").TextContent;
		Assert.DoesNotContain(":root {", css, StringComparison.Ordinal);
		Assert.DoesNotMatch("(?<!prefers-)color-scheme:", css);
		Assert.Equal(["moka-root", "moka-scheme-auto"], cut.Find(".moka-root").ClassList);
	}

	// The static renderer HTML-encodes text, and a style element never decodes it: quotes in font names, the plus in a
	// calc() and the quoted scope in the selector all broke.
	[Fact]
	public async Task TheStaticRenderer_WritesTheRulesAsWritten()
	{
		await using ServiceProvider services = new ServiceCollection()
			.AddLogging()
			.AddSingleton<IJSRuntime>(new NoJs())
			.BuildServiceProvider();
		await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

		string html = await renderer.Dispatcher.InvokeAsync(async () =>
		{
			HtmlRootComponent root = await renderer.RenderComponentAsync<MokaThemeProvider>(ParameterView.FromDictionary(
				new Dictionary<string, object?> { [nameof(MokaThemeProvider.AutoDetectColorScheme)] = true }));
			return root.ToHtmlString();
		});

		string css = html[html.IndexOf("<style", StringComparison.Ordinal)..html.IndexOf("</style>", StringComparison.Ordinal)];
		Assert.Contains("'JetBrains Mono'", css, StringComparison.Ordinal);
		Assert.Contains("calc(var(--moka-control-height) + var(--moka-spacing-md))", css, StringComparison.Ordinal);
		Assert.Matches("\\.moka-root\\[data-moka-theme=\"moka-theme-[0-9a-f]{32}\"\\] \\{", css);
		Assert.DoesNotContain("&#x27;", css, StringComparison.Ordinal);
		Assert.DoesNotContain("&quot;", css, StringComparison.Ordinal);
		Assert.DoesNotContain("&#x2B;", css, StringComparison.Ordinal);
	}

	// Blazor replaces the prerendered markup with the interactive render. When the two match, nothing flashes.
	[Fact]
	public void TheInteractiveRender_MatchesThePrerenderedOne()
	{
		AnswerLater();

		SetRendererInfo(Static);
		string prerendered = Render<MokaThemeProvider>(p => p
			.Add(x => x.AutoDetectColorScheme, true)
			.AddChildContent("<span>Hello</span>")).Markup;
		SetRendererInfo(Interactive);
		string interactive = Render<MokaThemeProvider>(p => p
			.Add(x => x.AutoDetectColorScheme, true)
			.AddChildContent("<span>Hello</span>")).Markup;

		Assert.Equal(Scope().Replace(prerendered, "SCOPE"), Scope().Replace(interactive, "SCOPE"));
	}

	[Fact]
	public async Task TheBrowsersAnswer_MovesTheClassAndTheCascade_NotTheTokens()
	{
		SetRendererInfo(Interactive);
		BunitJSModuleInterop module = JSInterop.SetupModule(ThemeModule);
		JSRuntimeInvocationHandler<bool> prefersDark = module.Setup<bool>("prefersDarkColorScheme", _ => true);
		module.Setup<int>("watchColorScheme", _ => true).SetResult(1);
		MokaTheme? received = null;

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.AutoDetectColorScheme, true)
			.Add(x => x.Theme, MokaTheme.Light)
			.AddChildContent<ThemeReceiver>(child => child.Add(x => x.OnThemeReceived, theme => received = theme)));
		string tokens = cut.Find("style").TextContent;
		Assert.Equal(MokaTheme.Default, received);

		await cut.InvokeAsync(() => prefersDark.SetResult(true));
		cut.WaitForAssertion(() => Assert.Contains("moka-dark", cut.Find(".moka-root").ClassList));
		Assert.Contains("moka-scheme-auto", cut.Find(".moka-root").ClassList);
		Assert.Equal(tokens, cut.Find("style").TextContent);

		await cut.Instance.OnColorSchemeChanged(false);

		Assert.DoesNotContain("moka-dark", cut.Find(".moka-root").ClassList);
		Assert.Equal(MokaTheme.DefaultLight, received);
		Assert.Equal(tokens, cut.Find("style").TextContent);
	}

	[Fact]
	public void WithANonce_TheRulesCarryIt_AndTheRootHasNoStyle()
	{
		SetRendererInfo(Static);
		AnswerLater();

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.AutoDetectColorScheme, true)
			.Add(x => x.Nonce, "abc123"));

		IElement root = cut.Find(".moka-root");
		IElement style = Assert.Single(cut.FindAll("style"));
		Assert.Equal("abc123", style.GetAttribute("nonce"));
		Assert.False(root.HasAttribute("style"));
		Assert.Contains(Scoped(root.GetAttribute("data-moka-theme")!), style.TextContent, StringComparison.Ordinal);
	}

	// A fixed appearance gives the same theme twice. The provider then has nothing to follow and draws that theme.
	[Fact]
	public void TheSameThemeTwice_RendersAsThatTheme()
	{
		SetRendererInfo(Static);
		AnswerLater();

		string auto = Render<MokaThemeProvider>(p => p
			.Add(x => x.AutoDetectColorScheme, true)
			.Add(x => x.DarkTheme, MokaTheme.GateLight)
			.Add(x => x.LightTheme, MokaTheme.GateLight)
			.AddChildContent("<span>Hello</span>")).Markup;
		string fixedTheme = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.GateLight)
			.AddChildContent("<span>Hello</span>")).Markup;

		Assert.Equal(fixedTheme, auto);
	}

	[Fact]
	public void WithoutAutoDetect_NothingFollowsTheOs()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.DarkTheme, MokaTheme.Gate)
			.Add(x => x.LightTheme, MokaTheme.GateLight));

		Assert.DoesNotContain("@media", cut.Find("style").TextContent, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-scheme-auto", cut.Find(".moka-root").ClassList);
	}

	// Works the cascade out in each scheme the way a browser does, from moka.css's :root rule, the page-wide rules and each
	// root's own declarations, and checks that every token lands on the theme that scheme asks for, at both roots.
	[Theory]
	[MemberData(nameof(NestedLayouts))]
	public void EveryToken_LandsOnTheSchemesTheme_InTheOuterAndTheNestedRoot(string outer, string inner)
	{
		SetRendererInfo(Static);
		AnswerLater();
		Provider outerProvider = Providers[outer];
		Provider innerProvider = Providers[inner];

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => outerProvider.Apply(p)
			.AddChildContent<MokaThemeProvider>(nested => innerProvider.Apply(nested).AddChildContent("<span>Nested</span>")));

		IReadOnlyList<IElement> roots = cut.FindAll(".moka-root");
		Dictionary<string, string> mokaRoot = new(ScopedCss.Declarations("moka.css", ":root"), StringComparer.Ordinal);
		foreach (bool dark in new[] { false, true })
		{
			string scheme = dark ? "dark" : "light";
			Dictionary<string, string> page = ProviderCascadeTests.Compute(
				ProviderCascadeTests.Merge(mokaRoot, ProviderCascadeTests.Declarations(PageTokens(cut, roots[0], scheme))), []);
			Dictionary<string, string> outerRoot = ProviderCascadeTests.Compute(
				ProviderCascadeTests.Declarations(RootTokens(cut, roots[0], scheme)), page);
			Dictionary<string, string> innerRoot = ProviderCascadeTests.Compute(
				ProviderCascadeTests.Declarations(RootTokens(cut, roots[1], scheme)), outerRoot);

			ProviderCascadeTests.AssertLands(outerProvider.ThemeFor(dark), outerRoot, $"{outer} as the outer provider, {scheme}");
			ProviderCascadeTests.AssertLands(innerProvider.ThemeFor(dark), innerRoot, $"{inner} inside {outer}, {scheme}");
		}

		Assert.Equal(outerProvider.Follows, roots[0].ClassList.Contains("moka-scheme-auto"));
		Assert.Equal(innerProvider.Follows, roots[1].ClassList.Contains("moka-scheme-auto"));
	}

	// :root's tokens in a scheme: the outermost's page-wide style element.
	private static string PageTokens(IRenderedComponent<MokaThemeProvider> cut, IElement root, string scheme)
	{
		string css = cut.FindAll("style")[0].TextContent;
		return css.Contains("@media", StringComparison.Ordinal)
			? Rule(css, scheme, Scoped(root.GetAttribute("data-moka-theme")!))
			: css[":root {".Length..^1];
	}

	// A root's own tokens in a scheme: its style attribute, or the media block of the style element scoped to it.
	private static string RootTokens(IRenderedComponent<MokaThemeProvider> cut, IElement root, string scheme)
	{
		if (root.GetAttribute("style") is { } style)
		{
			return style;
		}

		if (root.GetAttribute("data-moka-theme") is not { } scope)
		{
			return "";
		}

		string css = cut.FindAll("style").Single(s => s.TextContent.Contains(Scoped(scope), StringComparison.Ordinal)).TextContent;
		return Rule(css, scheme, Scoped(scope));
	}

	private static string Scoped(string scope) => $".moka-root[data-moka-theme=\"{scope}\"]";

	// The declarations of a rule inside the scheme's media block, or nothing when the block or the rule isn't there.
	private static string Rule(string css, string scheme, string selector)
	{
		int media = css.IndexOf($"@media (prefers-color-scheme: {scheme}) {{", StringComparison.Ordinal);
		if (media < 0)
		{
			return "";
		}

		int next = css.IndexOf("@media", media + 1, StringComparison.Ordinal);
		int end = next < 0 ? css.Length : next;
		int rule = css.IndexOf(selector + " {", media, end - media, StringComparison.Ordinal);
		if (rule < 0)
		{
			return "";
		}

		int open = rule + selector.Length + 2;
		return css[open..css.IndexOf('}', open)].Trim();
	}

	// The browser hasn't answered yet: what prerendering, a static page and the first interactive render show.
	private void AnswerLater()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ThemeModule);
		module.Setup<bool>("prefersDarkColorScheme", _ => true);
	}

	[GeneratedRegex("moka-theme-[0-9a-f]{32}")]
	private static partial Regex Scope();

	private sealed record Provider(MokaTheme Theme, MokaTheme? Dark = null, MokaTheme? Light = null)
	{
		public bool Follows => Dark is not null && !Equals(Dark, Light);

		public MokaTheme ThemeFor(bool dark) => Dark is null ? Theme : dark ? Dark : Light!;

		public ComponentParameterCollectionBuilder<MokaThemeProvider> Apply(ComponentParameterCollectionBuilder<MokaThemeProvider> builder)
		{
			builder.Add(x => x.Theme, Theme);
			return Dark is null
				? builder
				: builder.Add(x => x.AutoDetectColorScheme, true).Add(x => x.DarkTheme, Dark).Add(x => x.LightTheme, Light!);
		}
	}

	private sealed class ThemeReceiver : ComponentBase
	{
		[CascadingParameter] public MokaTheme? Theme { get; set; }

		[Parameter] public Action<MokaTheme?>? OnThemeReceived { get; set; }

		protected override void OnParametersSet() => OnThemeReceived?.Invoke(Theme);
	}

	private sealed class NoJs : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			new(default(TValue)!);
	}
}
