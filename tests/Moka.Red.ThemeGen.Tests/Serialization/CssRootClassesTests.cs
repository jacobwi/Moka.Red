using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Serialization;

namespace Moka.Red.ThemeGen.Tests.Serialization;

// The CSS export names the classes MokaThemeProvider puts on its root, which it works out where ThemeGen can't call it.
public sealed class CssRootClassesTests : BunitContext
{
	private const string Marker = "/* Root classes: ";

	public CssRootClassesTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string, MokaTheme> Themes => new()
	{
		{ "red", MokaTheme.Red },
		{ "red light", MokaTheme.RedLight },
		{ "original dark", MokaTheme.Dark },
		{ "original light", MokaTheme.Light },
		{
			"every class", MokaTheme.Gate.WithPopups(MokaPopupLook.Glass).WithButtons(MokaButtonLook.Pill).WithDisabledButtons(MokaDisabledButtonLook.Outline)
				.WithTooltips(MokaTooltipLook.Terminal).WithTooltipArrows().WithTooltipMotion(MokaTooltipMotion.Grow).WithTooltipSize(MokaTooltipSize.Roomy)
		}
	};

	[Theory]
	[MemberData(nameof(Themes))]
	public void TheCssExport_NamesTheClassesTheOutermostProviderWrites(string name, MokaTheme theme)
	{
		IRenderedComponent<MokaThemeProvider> provider = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, theme));
		string css = MokaThemeSerializer.ToCss(theme);
		string line = css[Marker.Length..css.IndexOf(" */", StringComparison.Ordinal)];

		Assert.True(css.StartsWith(Marker, StringComparison.Ordinal), name);
		Assert.Equal(
			provider.Find(".moka-root").ClassList.Order(StringComparer.Ordinal),
			line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal));
	}
}
