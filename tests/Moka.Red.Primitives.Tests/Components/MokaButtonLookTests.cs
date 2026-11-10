using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Primitives.Button;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     A button's look comes from the theme, as a class on the outermost theme root, or from its own <c>Look</c>, as
///     <c>data-moka-look</c>. Both only reach CSS, so most of this reads the stylesheet.
/// </summary>
public sealed partial class MokaButtonLookTests : BunitContext
{
	private const string ButtonCss = "MokaButton.razor.css";

	private static readonly string[] Looks = ["raised", "soft", "pill", "glow", "offset", "gradient", "ghost"];

	// The fallback first, so a browser without relative colors keeps it.
	private static readonly string[] GradientEnds =
	[
		"color-mix(in srgb, var(--_btn-from) 62%, var(--moka-color-warning))",
		"oklch(from var(--_btn-from) calc(l + 0.05) c calc(h + 32))"
	];

	[Theory]
	[InlineData(MokaButtonLook.Standard, "standard")]
	[InlineData(MokaButtonLook.Raised, "raised")]
	[InlineData(MokaButtonLook.Soft, "soft")]
	[InlineData(MokaButtonLook.Pill, "pill")]
	[InlineData(MokaButtonLook.Glow, "glow")]
	[InlineData(MokaButtonLook.Offset, "offset")]
	[InlineData(MokaButtonLook.Gradient, "gradient")]
	[InlineData(MokaButtonLook.Ghost, "ghost")]
	public void Look_IsWrittenOnTheButton(MokaButtonLook look, string name)
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Look, look)
			.AddChildContent("Save"));

		Assert.Equal(name, cut.Find("button").GetAttribute("data-moka-look"));
	}

	[Fact]
	public void Look_OnALink_IsWrittenOnTheAnchor()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Look, MokaButtonLook.Glow)
			.Add(x => x.Href, "/settings")
			.AddChildContent("Settings"));

		Assert.Equal("glow", cut.Find("a").GetAttribute("data-moka-look"));
	}

	[Fact]
	public void NoLook_WritesNoAttribute()
	{
		IRenderedComponent<MokaButton> button = Render<MokaButton>(p => p.AddChildContent("Save"));
		IRenderedComponent<MokaButton> link = Render<MokaButton>(p => p
			.Add(x => x.Href, "/settings")
			.AddChildContent("Settings"));

		Assert.False(button.Find("button").HasAttribute("data-moka-look"));
		Assert.False(link.Find("a").HasAttribute("data-moka-look"));
	}

	[Fact]
	public void Look_LeavesTheClassesAsTheyAre()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Look, MokaButtonLook.Pill)
			.AddChildContent("Save"));

		Assert.Equal("moka-btn moka-btn--filled moka-btn--primary moka-btn--md", cut.Find("button").ClassName);
	}

	[Fact]
	public void UnderATheme_AButtonWithoutALook_IsLeftToTheThemesClass()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red.WithButtons(MokaButtonLook.Raised))
			.AddChildContent<MokaButton>(button => button.AddChildContent("Save")));

		Assert.Contains("moka-buttons-raised", cut.Find(".moka-root").ClassList);
		Assert.False(cut.Find("button").HasAttribute("data-moka-look"));
	}

	[Fact]
	public void UnderATheme_AButtonsOwnLook_IsWrittenOnIt()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red.WithButtons(MokaButtonLook.Raised))
			.AddChildContent<MokaButton>(button => button
				.Add(x => x.Look, MokaButtonLook.Pill)
				.AddChildContent("Save")));

		Assert.Equal("pill", cut.Find("button").GetAttribute("data-moka-look"));
	}

	[Fact]
	public void TheOriginalDesign_HasNoButtonLookClass()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Dark.WithButtons(MokaButtonLook.Raised))
			.AddChildContent<MokaButton>(button => button.AddChildContent("Save")));

		IElement root = cut.Find(".moka-root");
		Assert.DoesNotContain("moka-gate", root.ClassList);
		Assert.DoesNotContain(root.ClassList, c => c.StartsWith("moka-buttons-", StringComparison.Ordinal));
	}

	// Every look rule is under .moka-gate, so a Look set on a button under Dark or Light changes nothing.
	[Fact]
	public void EveryLookRule_IsUnderTheNewDesign()
	{
		List<string> found = [.. Selectors()
			.Where(s => s.Contains("data-moka-look", StringComparison.Ordinal) || s.Contains("moka-buttons-", StringComparison.Ordinal))
			.Where(s => !s.StartsWith(".moka-gate", StringComparison.Ordinal))];

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	[Fact]
	public void TheThemesSelectors_SkipAButtonWithALookOfItsOwn()
	{
		List<string> theme = [.. Selectors().Where(s => s.Contains("moka-buttons-", StringComparison.Ordinal))];

		Assert.NotEmpty(theme);
		Assert.All(theme, s =>
		{
			Assert.StartsWith(".moka-gate.moka-buttons-", s, StringComparison.Ordinal);
			Assert.Contains(":not([data-moka-look])", s, StringComparison.Ordinal);
		});
	}

	// The theme's selector and the button's own come in pairs, so neither way of asking for a look misses a rule.
	[Fact]
	public void EveryLook_HasTheThemesSelectorAndTheButtonsOwnInEveryRule()
	{
		foreach (string look in Looks)
		{
			string themeClass = $".moka-gate.moka-buttons-{look} ";
			string own = $"[data-moka-look=\"{look}\"]";
			List<List<string>> rules = [.. Rules().Select(r => r.Selectors)
				.Where(s => s.Any(x => x.StartsWith(themeClass, StringComparison.Ordinal) || x.Contains(own, StringComparison.Ordinal)))];

			Assert.True(rules.Count > 0, $"No rules for the {look} look.");
			Assert.All(rules, selectors => Assert.Equal(
				selectors.Count(x => x.StartsWith(themeClass, StringComparison.Ordinal)),
				selectors.Count(x => x.Contains(own, StringComparison.Ordinal))));
		}
	}

	[Fact]
	public void Standard_HasNoRules()
	{
		string css = File.ReadAllText(CssPath(ButtonCss));

		Assert.DoesNotContain("moka-buttons-standard", css, StringComparison.Ordinal);
		Assert.DoesNotContain("data-moka-look=\"standard\"", css, StringComparison.Ordinal);
	}

	// A look that draws its own shadow outranks the focus rule, so it has to carry the focus ring itself.
	[Fact]
	public void EveryLookShadow_KeepsTheFocusRing()
	{
		Assert.Equal("var(--moka-focus-ring)",
			ScopedCss.Declarations(ButtonCss, ".moka-gate .moka-btn:focus-visible")["--_btn-focus"]);

		List<string> found = [.. Rules()
			.Where(r => r.Selectors.Any(s => s.Contains("data-moka-look", StringComparison.Ordinal)))
			.SelectMany(r => r.Declarations.Where(d => d.Property == "box-shadow"))
			.Where(d => !d.Value.StartsWith("var(--_btn-focus, 0 0 transparent), ", StringComparison.Ordinal))
			.Select(d => d.Value)];

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	// WithMainButton still decides how the main button is filled, except under Glow and Gradient, which redraw it.
	[Theory]
	[InlineData("raised")]
	[InlineData("soft")]
	[InlineData("pill")]
	[InlineData("offset")]
	[InlineData("ghost")]
	public void TheMainButton_KeepsTheMainButtonFill(string look)
	{
		List<string> found = [.. Rules()
			.Where(r => r.Declarations.Any(d => d.Property is "background-color" or "background"))
			.SelectMany(r => r.Selectors)
			.Where(s => s.Contains($"[data-moka-look=\"{look}\"]", StringComparison.Ordinal)
				|| s.StartsWith($".moka-gate.moka-buttons-{look} ", StringComparison.Ordinal))
			.Where(s => MainButton().IsMatch(s))];

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	// Under a button group's rules, which square the inner corners and give the end buttons the group's.
	[Fact]
	public void Pill_IsFullyRound_BelowAButtonGroup()
	{
		Assert.Equal("var(--moka-radius-full)", ScopedCss.Declarations(ButtonCss,
			".moka-gate.moka-buttons-pill :where(.moka-btn:not([data-moka-look]))")["border-radius"]);
		Assert.Equal("var(--moka-radius-full)", ScopedCss.Declarations(ButtonCss,
			".moka-gate .moka-btn:where([data-moka-look=\"pill\"])")["border-radius"]);
		Assert.Equal("var(--moka-radius-full)", ScopedCss.Declarations("MokaButtonGroup.razor.css",
			".moka-gate.moka-buttons-pill .moka-btn-group:not(.moka-btn-group--column)")["border-radius"]);
	}

	[Fact]
	public void Pill_LeavesAnIconOnlyButtonSquare()
	{
		List<string> padded = [.. Rules()
			.Where(r => r.Declarations.Any(d => d.Property == "padding-inline"))
			.SelectMany(r => r.Selectors)];

		Assert.Equal(8, padded.Count);
		Assert.All(padded, s => Assert.Contains(":not(.moka-btn--icon-only)", s, StringComparison.Ordinal));
	}

	// Down a pixel, and without the move when reduced motion is asked for, which comes later.
	[Fact]
	public void Raised_PressesIn()
	{
		const string pressed =
			".moka-gate .moka-btn[data-moka-look=\"raised\"]:is(.moka-btn--filled, .moka-btn--outlined, .moka-btn--soft):active:not(.moka-btn--disabled)";

		Assert.Equal(["translateY(1px)", "none"], Values(pressed, "transform"));
		Assert.Contains("inset", Declarations(pressed)["box-shadow"], StringComparison.Ordinal);
	}

	[Fact]
	public void Offset_DrawsASolidShadowInTheTextColor()
	{
		Dictionary<string, string> css = Declarations(
			".moka-gate .moka-btn[data-moka-look=\"offset\"]:is(.moka-btn--filled, .moka-btn--outlined, .moka-btn--soft)");

		Assert.Equal("var(--moka-color-on-surface)", css["border-color"]);
		Assert.Equal("var(--_btn-focus, 0 0 transparent), var(--_btn-offset) var(--_btn-offset) 0 var(--moka-color-on-surface)",
			css["box-shadow"]);
	}

	[Fact]
	public void Gradient_RunsFromTheButtonsColor_WithAFallbackWhereRelativeColorsDontWork()
	{
		const string colored = ".moka-gate .moka-btn[data-moka-look=\"gradient\"].moka-btn--filled:is(.moka-btn--primary, .moka-btn--error, .moka-btn--warning, .moka-btn--success, .moka-btn--info)";

		Assert.Equal("var(--moka-button-primary-bg)", ScopedCss.Declarations(ButtonCss,
			".moka-gate .moka-btn[data-moka-look=\"gradient\"].moka-btn--filled.moka-btn--primary")["--_btn-from"]);
		Assert.Equal("linear-gradient(135deg, var(--_btn-from), var(--_btn-to))",
			Declarations(colored)["background-image"]);
		Assert.Equal(GradientEnds, Values(colored, "--_btn-to"));
		Assert.Contains("@supports (color: oklch(from transparent l c h))", File.ReadAllText(CssPath(ButtonCss)),
			StringComparison.Ordinal);
	}

	[Fact]
	public void TheLooksThatMove_StandStillForReducedMotion()
	{
		string css = Comment().Replace(File.ReadAllText(CssPath(ButtonCss)), " ");
		int start = css.IndexOf("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal);
		Assert.True(start >= 0, "No reduced motion block.");
		string reduced = css[start..];

		foreach (string look in new[] { "raised", "offset" })
		{
			Assert.Contains($".moka-gate.moka-buttons-{look} ", reduced, StringComparison.Ordinal);
			Assert.Contains($"[data-moka-look=\"{look}\"]", reduced, StringComparison.Ordinal);
		}

		Assert.Contains("transform: none", reduced, StringComparison.Ordinal);
	}

	private static string CssPath(string file) => Path.Combine(AppContext.BaseDirectory, "Css", file);

	// ScopedCss splits a selector list at every comma, and these selectors have commas inside :is().
	private static Dictionary<string, string> Declarations(string selector)
	{
		Dictionary<string, string> declarations = new(StringComparer.Ordinal);
		foreach ((string property, string value) in Rules().Where(r => r.Selectors.Contains(selector)).SelectMany(r => r.Declarations))
		{
			declarations[property] = value;
		}

		return declarations;
	}

	private static List<string> Values(string selector, string property) =>
		[.. Rules().Where(r => r.Selectors.Contains(selector)).SelectMany(r => r.Declarations)
			.Where(d => d.Property == property).Select(d => d.Value)];

	private static IEnumerable<string> Selectors() => Rules().SelectMany(r => r.Selectors);

	private static IEnumerable<(List<string> Selectors, List<(string Property, string Value)> Declarations)> Rules()
	{
		string css = Comment().Replace(File.ReadAllText(CssPath(ButtonCss)), " ");
		foreach (Match rule in Rule().Matches(css))
		{
			List<(string, string)> declarations = [];
			foreach (string declaration in rule.Groups["body"].Value.Split(';'))
			{
				int colon = declaration.IndexOf(':', StringComparison.Ordinal);
				if (colon > 0)
				{
					declarations.Add((declaration[..colon].Trim(), declaration[(colon + 1)..].Trim()));
				}
			}

			yield return ([.. TopLevelSelectors(rule.Groups["selectors"].Value)], declarations);
		}
	}

	// A comma inside :is(), :not() or :where() does not separate selectors.
	private static IEnumerable<string> TopLevelSelectors(string head)
	{
		int depth = 0, start = 0;
		for (int i = 0; i < head.Length; i++)
		{
			switch (head[i])
			{
				case '(':
					depth++;
					break;
				case ')':
					depth--;
					break;
				case ',' when depth == 0:
					yield return head[start..i].Trim();
					start = i + 1;
					break;
			}
		}

		yield return head[start..].Trim();
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"(?<selectors>[^{}]+)\{(?<body>[^{}]*)\}")]
	private static partial Regex Rule();

	[GeneratedRegex(@"\.moka-btn--filled(\.moka-btn--primary(?![\w-])|:is\([^)]*\.moka-btn--primary(?![\w-]))")]
	private static partial Regex MainButton();
}
