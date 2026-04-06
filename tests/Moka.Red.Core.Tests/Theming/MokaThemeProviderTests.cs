using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
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
		// CSS variables are injected via <style>:root { ... }</style> in HeadContent.
		// bUnit doesn't render HeadContent, so we verify the theme generates correct CSS.
		string css = MokaTheme.Light.ToCssVariables();
		Assert.Contains("--moka-color-primary: #d32f2f", css, StringComparison.Ordinal);
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

	/// <summary>Helper component that captures the cascaded theme.</summary>
	private sealed class ThemeReceiver : ComponentBase
	{
		[CascadingParameter] public MokaTheme? Theme { get; set; }

		[Parameter] public Action<MokaTheme?>? OnThemeReceived { get; set; }

		protected override void OnParametersSet() => OnThemeReceived?.Invoke(Theme);
	}
}
