using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;

namespace Moka.Red.Diagnostics.Tests.Components;

public class ThemeInspectorPanelTests : BunitContext
{
	public ThemeInspectorPanelTests()
	{
		Services.AddMokaDiagnostics();
		JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true);
	}

	[Fact]
	public void Renders_WhenThemeProvided()
	{
		IRenderedComponent<ThemeInspectorPanel> cut = Render<ThemeInspectorPanel>(parameters => parameters
			.Add(p => p.Theme, MokaTheme.Light));

		IElement root = cut.Find(".moka-diag-theme");
		Assert.NotNull(root);
	}

	[Fact]
	public void DoesNotRender_WhenThemeNull()
	{
		IRenderedComponent<ThemeInspectorPanel> cut = Render<ThemeInspectorPanel>();

		Assert.Empty(cut.Markup.Trim());
	}

	[Fact]
	public void Shows_AllTokenGroups()
	{
		IRenderedComponent<ThemeInspectorPanel> cut = Render<ThemeInspectorPanel>(parameters => parameters
			.Add(p => p.Theme, MokaTheme.Light));

		IReadOnlyList<IElement> groups = cut.FindAll(".moka-diag-group-header");
		Assert.Equal(4, groups.Count);
	}

	[Fact]
	public void Shows_LightModeIndicator()
	{
		IRenderedComponent<ThemeInspectorPanel> cut = Render<ThemeInspectorPanel>(parameters => parameters
			.Add(p => p.Theme, MokaTheme.Light));

		IElement modeText = cut.Find(".moka-diag-theme-mode");
		Assert.Contains("Light", modeText.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void Shows_DarkModeIndicator_WhenDarkTheme()
	{
		IRenderedComponent<ThemeInspectorPanel> cut = Render<ThemeInspectorPanel>(parameters => parameters
			.Add(p => p.Theme, MokaTheme.Dark));

		IElement modeText = cut.Find(".moka-diag-theme-mode");
		Assert.Contains("Dark", modeText.TextContent, StringComparison.Ordinal);
	}

	// The panel wrote token values into the style attribute as they were. A theme is anyone's to
	// build, so a value could add declarations of its own, or load a url() as the swatch background.
	[Fact]
	public void TokenValues_ThatAreNotSafe_GetNoPreviewStyle()
	{
		MokaTheme theme = MokaTheme.Dark with
		{
			Palette = MokaTheme.Dark.Palette with
			{
				Primary = "red; position: fixed; inset: 0",
				Secondary = "url(https://example.com/pixel.png)"
			},
			Spacing = MokaTheme.Dark.Spacing with { Md = "8px; position: fixed" }
		};

		IRenderedComponent<ThemeInspectorPanel> cut = Render<ThemeInspectorPanel>(parameters => parameters
			.Add(p => p.Theme, theme));

		Assert.Null(Preview(cut, "--moka-color-primary").GetAttribute("style"));
		Assert.Null(Preview(cut, "--moka-color-secondary").GetAttribute("style"));
		Assert.Null(Preview(cut, "--moka-spacing-md").GetAttribute("style"));
	}

	[Fact]
	public void TokenValues_ThatAreSafe_AreShown()
	{
		MokaTheme theme = MokaTheme.Dark with
		{
			Palette = MokaTheme.Dark.Palette with { Primary = "rgb(239 83 80 / 90%)" },
			Spacing = MokaTheme.Dark.Spacing with { Md = "calc(0.5rem + 1px)" }
		};

		IRenderedComponent<ThemeInspectorPanel> cut = Render<ThemeInspectorPanel>(parameters => parameters
			.Add(p => p.Theme, theme));

		Assert.Equal("background-color: rgb(239 83 80 / 90%)", Preview(cut, "--moka-color-primary").GetAttribute("style"));
		Assert.Equal("width: calc(0.5rem + 1px)", Preview(cut, "--moka-spacing-md").GetAttribute("style"));
	}

	private static IElement Preview(IRenderedComponent<ThemeInspectorPanel> cut, string variable) =>
		cut.FindAll(".moka-diag-token-row")
			.Single(row => row.QuerySelector(".moka-diag-token-name")!.TextContent == variable)
			.QuerySelector(".moka-diag-color-swatch, .moka-diag-ruler")!;

	[Fact]
	public void TokenRows_ContainCssVariableNames()
	{
		IRenderedComponent<ThemeInspectorPanel> cut = Render<ThemeInspectorPanel>(parameters => parameters
			.Add(p => p.Theme, MokaTheme.Light));

		IReadOnlyList<IElement> tokenNames = cut.FindAll(".moka-diag-token-name");
		Assert.Contains(tokenNames, t => t.TextContent.Contains("--moka-color-primary", StringComparison.Ordinal));
		Assert.Contains(tokenNames, t => t.TextContent.Contains("--moka-font-family", StringComparison.Ordinal));
		Assert.Contains(tokenNames, t => t.TextContent.Contains("--moka-spacing-md", StringComparison.Ordinal));
		Assert.Contains(tokenNames, t => t.TextContent.Contains("--moka-radius-md", StringComparison.Ordinal));
	}
}
