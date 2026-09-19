using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

public class MokaThemeProviderTests : BunitContext
{
	[Fact]
	public void Renders_MokaRoot_Div()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(parameters => parameters
			.AddChildContent("<span>Hello</span>"));

		IElement root = cut.Find(".moka-root");
		Assert.NotNull(root);
	}

	[Fact]
	public void Renders_ChildContent()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(parameters => parameters
			.AddChildContent("<span id=\"child\">Content</span>"));

		IElement child = cut.Find("#child");
		Assert.Equal("Content", child.TextContent);
	}

	[Fact]
	public void Generates_CssVariables()
	{
		string css = MokaTheme.Light.ToCssVariables();
		Assert.Contains("--moka-color-primary: #d32f2f", css, StringComparison.Ordinal);
	}

	// Through HeadContent these were dropped whenever anything else on the page used HeadContent
	// (only the last one reaches the head), and never arrived in a host without a HeadOutlet.
	[Fact]
	public void TheProvider_RendersTheStylesheetAndRootTokensItself()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.AddChildContent("<span>Hello</span>"));

		Assert.Empty(cut.FindComponents<HeadContent>());
		Assert.Single(cut.FindAll("link[rel=stylesheet][href='_content/Moka.Red.Core/moka.css']"));
		IElement rootStyle = Assert.Single(cut.FindAll("style"));
		Assert.StartsWith(":root {", rootStyle.TextContent, StringComparison.Ordinal);
		Assert.Contains("--moka-color-primary", rootStyle.TextContent, StringComparison.Ordinal);
	}

	// A nested provider themes its own subtree. Its tokens must not replace the page-wide ones.
	[Fact]
	public void ANestedProvider_LeavesThePageWideStylesToTheOutermost()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Light)
			.AddChildContent<MokaThemeProvider>(inner => inner
				.Add(x => x.Theme, MokaTheme.Dark)
				.AddChildContent("<span id=\"inner\">Dark</span>")));

		Assert.Single(cut.FindAll("link[href='_content/Moka.Red.Core/moka.css']"));
		IElement rootStyle = Assert.Single(cut.FindAll("style"));
		Assert.Contains(MokaTheme.Light.ToCssVariables(), rootStyle.TextContent, StringComparison.Ordinal);

		IReadOnlyList<IElement> roots = cut.FindAll(".moka-root");
		Assert.Equal(2, roots.Count);
		Assert.Contains("moka-dark", roots[1].ClassName, StringComparison.Ordinal);
		Assert.Equal(MokaTheme.Dark.ToCssVariables(), roots[1].GetAttribute("style"));
	}

	[Fact]
	public void DarkTheme_Adds_DarkClass()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(parameters => parameters
			.Add(p => p.Theme, MokaTheme.Dark));

		IElement root = cut.Find(".moka-root");
		Assert.Contains("moka-dark", root.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void LightTheme_NoDarkClass()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(parameters => parameters
			.Add(p => p.Theme, MokaTheme.Light));

		IElement root = cut.Find(".moka-root");
		Assert.DoesNotContain("moka-dark", root.ClassName ?? string.Empty, StringComparison.Ordinal);
	}

	[Fact]
	public void Cascades_Theme_ToChildren()
	{
		MokaTheme? receivedTheme = null;

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(parameters => parameters
			.Add(p => p.Theme, MokaTheme.Dark)
			.AddChildContent<ThemeReceiver>(child => child
				.Add(p => p.OnThemeReceived, t => receivedTheme = t)));

		Assert.NotNull(receivedTheme);
		Assert.True(receivedTheme!.IsDark);
	}

	// A strict content security policy blocks inline style attributes and un-nonced style elements.
	[Fact]
	public void Nonce_MovesTheTokensIntoANoncedStyle_ScopedToTheRoot()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Nonce, "abc123")
			.AddChildContent("<span>Hello</span>"));

		IElement root = cut.Find(".moka-root");
		string? scope = root.GetAttribute("data-moka-theme");
		Assert.False(root.HasAttribute("style"));
		Assert.False(string.IsNullOrEmpty(scope));

		// Every style element carries the nonce: the page-wide :root one and the one for this subtree.
		IReadOnlyList<IElement> styles = cut.FindAll("style");
		Assert.Equal(2, styles.Count);
		Assert.All(styles, style => Assert.Equal("abc123", style.GetAttribute("nonce")));
		IElement scoped = Assert.Single(styles, style => style.TextContent.Contains($"[data-moka-theme=\"{scope}\"]", StringComparison.Ordinal));
		Assert.Contains("--moka-color-primary", scoped.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutANonce_TheTokensStayOnTheRoot()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.AddChildContent("<span>Hello</span>"));

		IElement root = cut.Find(".moka-root");
		Assert.Contains("--moka-color-primary", root.GetAttribute("style"), StringComparison.Ordinal);
		Assert.False(root.HasAttribute("data-moka-theme"));
	}

	// The detected theme used to be written into the Theme parameter, so the next parent render put
	// the parent's theme back. Detection also went through eval.
	[Fact]
	public void AutoDetect_KeepsTheDetectedThemeWhenTheParentRendersAgain()
	{
		BunitJSModuleInterop module = SetupThemeModule(prefersDark: true);

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.AutoDetectColorScheme, true)
			.Add(x => x.Theme, MokaTheme.Light));
		cut.WaitForAssertion(() => Assert.Contains("moka-dark", cut.Find(".moka-root").ClassName, StringComparison.Ordinal));

		cut.Render(p => p.Add(x => x.Theme, MokaTheme.Light));

		Assert.Contains("moka-dark", cut.Find(".moka-root").ClassName, StringComparison.Ordinal);
		module.VerifyInvoke("watchColorScheme");
		Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "eval");
	}

	[Fact]
	public async Task AutoDetect_FollowsTheOsWhenItChanges()
	{
		SetupThemeModule(prefersDark: true);
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.AutoDetectColorScheme, true));
		cut.WaitForAssertion(() => Assert.Contains("moka-dark", cut.Find(".moka-root").ClassName, StringComparison.Ordinal));

		await cut.Instance.OnColorSchemeChanged(false);

		Assert.DoesNotContain("moka-dark", cut.Find(".moka-root").ClassName ?? string.Empty, StringComparison.Ordinal);
	}

	// The :root tokens are written into a <style> element, so a theme value with a brace ended the rule
	// and styled the whole page.
	[Fact]
	public void AThemeValueThatCouldEndTheRule_StaysOutOfTheStyleElement()
	{
		MokaTheme theme = MokaTheme.Dark with
		{
			Palette = MokaPalette.Dark with { Surface = "#0c0c10} body { display: none } :root { --x: 1" }
		};

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, theme));

		string css = Assert.Single(cut.FindAll("style")).TextContent;
		Assert.Equal(1, css.Count(c => c == '{'));
		Assert.Equal(1, css.Count(c => c == '}'));
		Assert.DoesNotContain("body", css, StringComparison.Ordinal);
		Assert.DoesNotContain("--moka-color-surface:", css, StringComparison.Ordinal);
	}

	private BunitJSModuleInterop SetupThemeModule(bool prefersDark)
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-theme.js");
		module.Setup<bool>("prefersDarkColorScheme", _ => true).SetResult(prefersDark);
		module.Setup<int>("watchColorScheme", _ => true).SetResult(1);
		return module;
	}

	/// <summary>Helper component that captures the cascaded theme.</summary>
	private sealed class ThemeReceiver : ComponentBase
	{
		[CascadingParameter] public MokaTheme? Theme { get; set; }

		[Parameter] public Action<MokaTheme?>? OnThemeReceived { get; set; }

		protected override void OnParametersSet() => OnThemeReceived?.Invoke(Theme);
	}
}
