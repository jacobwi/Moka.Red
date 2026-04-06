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
